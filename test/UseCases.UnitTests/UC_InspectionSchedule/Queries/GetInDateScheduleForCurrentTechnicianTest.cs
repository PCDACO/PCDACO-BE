using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_InspectionSchedule.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_InspectionSchedule.Queries
{
    [Collection("Test Collection")]
    public class GetInDateScheduleForCurrentTechnicianTest(DatabaseTestBase fixture)
        : IAsyncLifetime
    {
        private readonly AppDBContext _dbContext = fixture.DbContext;
        private readonly CurrentUser _currentUser = fixture.CurrentUser;
        private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
        private readonly IAesEncryptionService _aesEncryptionService = fixture.AesEncryptionService;
        private readonly IKeyManagementService _keyManagementService = fixture.KeyManagementService;
        private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync() => await _resetDatabase();

        [Fact]
        public async Task Handle_UserNotTechnician_ReturnsForbidden()
        {
            // Arrange
            var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
            var user = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
            _currentUser.SetUser(user);

            var handler = new GetInDateScheduleForCurrentTechnician.Handler(
                _dbContext,
                _currentUser,
                _aesEncryptionService,
                _keyManagementService,
                _encryptionSettings
            );
            var query = new GetInDateScheduleForCurrentTechnician.Query();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultStatus.Forbidden, result.Status);
            Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
        }

        [Fact]
        public async Task Handle_NoSchedules_ReturnsEmptyList()
        {
            // Arrange
            var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
                _dbContext,
                "Technician"
            );
            var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
            _currentUser.SetUser(technician);

            var handler = new GetInDateScheduleForCurrentTechnician.Handler(
                _dbContext,
                _currentUser,
                _aesEncryptionService,
                _keyManagementService,
                _encryptionSettings
            );
            var query = new GetInDateScheduleForCurrentTechnician.Query();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Empty(result.Value.Items);
            Assert.Equal(0, result.Value.TotalItems);
            Assert.False(result.Value.HasNext);
        }

        [Fact]
        public async Task Handle_WithSchedules_ReturnsPaginatedSchedules()
        {
            // Arrange
            var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
                _dbContext,
                "Technician"
            );
            var technician = await TestDataCreateUser.CreateTestUser(
                _dbContext,
                technicianRole,
                "tech@test.com",
                "John Doe",
                "0989379889",
                avatarUrl: "avatar.jpg"
            );
            _currentUser.SetUser(technician);

            // Create prerequisites
            var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
            var owner = await TestDataCreateUser.CreateTestUser(
                _dbContext,
                ownerRole,
                "owner@test.com",
                "Jane Smith",
                "0789970768",
                avatarUrl: "avatar.jpg"
            );
            var carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Pending");
            var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
            var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
            var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
                _dbContext,
                "Automatic"
            );
            var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

            // Create car with images
            var car = await TestDataCreateCar.CreateTestCarWithImages(
                _dbContext,
                owner.Id,
                model.Id,
                transmissionType,
                fuelType,
                carStatus,
                new[] { "image1.jpg", "image2.jpg" },
                aesEncryptionService: _aesEncryptionService,
                keyManagementService: _keyManagementService,
                encryptionSettings: _encryptionSettings
            );

            // Create pending status
            var pendingStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
                _dbContext,
                "Pending"
            );

            // Create multiple schedules
            var today = DateTimeOffset.UtcNow;
            var schedules = new List<InspectionSchedule>();
            for (int i = 0; i < 15; i++)
            {
                var schedule = new InspectionSchedule
                {
                    TechnicianId = technician.Id,
                    CarId = car.Id,
                    InspectionStatusId = pendingStatus.Id,
                    InspectionAddress = $"123 Main St {i + 1}",
                    InspectionDate = today,
                };
                schedules.Add(schedule);
            }
            await _dbContext.InspectionSchedules.AddRangeAsync(schedules);
            await _dbContext.SaveChangesAsync();

            var handler = new GetInDateScheduleForCurrentTechnician.Handler(
                _dbContext,
                _currentUser,
                _aesEncryptionService,
                _keyManagementService,
                _encryptionSettings
            );
            var query = new GetInDateScheduleForCurrentTechnician.Query(
                PageNumber: 1,
                PageSize: 10
            );

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal(10, result.Value.Items.Count());
            Assert.Equal(15, result.Value.TotalItems);
            Assert.True(result.Value.HasNext);
            Assert.Equal(1, result.Value.PageNumber);
            Assert.Equal(10, result.Value.PageSize);

            // Verify first schedule data
            var firstSchedule = result.Value.Items.First();
            Assert.Equal("John Doe", firstSchedule.TechnicianName);
            Assert.Equal(today, firstSchedule.InspectionDate);
            Assert.Equal("123 Main St 15", firstSchedule.InspectionAddress);

            // Verify car details
            var carDetails = firstSchedule.Cars.First();
            Assert.Equal(car.Id, carDetails.Id);
            Assert.Equal(model.Id, carDetails.ModelId);
            Assert.Equal(model.Name, carDetails.ModelName);
            Assert.Equal(manufacturer.Name, carDetails.ManufacturerName);
            Assert.Equal(2, carDetails.Images.Length);

            // Verify owner details
            Assert.Equal(owner.Id, carDetails.Owner.Id);
            Assert.Equal("Jane Smith", carDetails.Owner.Name);
            Assert.Equal("avatar.jpg", carDetails.Owner.AvatarUrl);
        }

        [Fact]
        public async Task Handle_LastPage_ReturnsHasNextFalse()
        {
            // Arrange
            var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
                _dbContext,
                "Technician"
            );
            var technician = await TestDataCreateUser.CreateTestUser(
                _dbContext,
                technicianRole,
                "tech@test.com",
                "John Doe",
                "0979377789",
                avatarUrl: "avatar.jpg"
            );
            _currentUser.SetUser(technician);

            // Create prerequisites
            var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
            var owner = await TestDataCreateUser.CreateTestUser(
                _dbContext,
                ownerRole,
                "owner@test.com",
                "Jane Smith",
                "0989397789",
                avatarUrl: "avatar.jpg"
            );
            var carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Pending");
            var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
            var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
            var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
                _dbContext,
                "Automatic"
            );
            var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

            // Create car with images
            var car = await TestDataCreateCar.CreateTestCarWithImages(
                _dbContext,
                owner.Id,
                model.Id,
                transmissionType,
                fuelType,
                carStatus,
                new[] { "image1.jpg", "image2.jpg" },
                aesEncryptionService: _aesEncryptionService,
                keyManagementService: _keyManagementService,
                encryptionSettings: _encryptionSettings
            );

            // Create pending status
            var pendingStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
                _dbContext,
                "Pending"
            );

            // Create multiple schedules
            var today = DateTimeOffset.UtcNow;
            var schedules = new List<InspectionSchedule>();
            for (int i = 0; i < 15; i++)
            {
                var schedule = new InspectionSchedule
                {
                    TechnicianId = technician.Id,
                    CarId = car.Id,
                    InspectionStatusId = pendingStatus.Id,
                    InspectionAddress = $"123 Main St {i + 1}",
                    InspectionDate = today,
                };
                schedules.Add(schedule);
            }
            await _dbContext.InspectionSchedules.AddRangeAsync(schedules);
            await _dbContext.SaveChangesAsync();

            var handler = new GetInDateScheduleForCurrentTechnician.Handler(
                _dbContext,
                _currentUser,
                _aesEncryptionService,
                _keyManagementService,
                _encryptionSettings
            );
            var query = new GetInDateScheduleForCurrentTechnician.Query(
                PageNumber: 2,
                PageSize: 10
            );

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(ResultStatus.Ok, result.Status);
            Assert.Equal(5, result.Value.Items.Count());
            Assert.Equal(15, result.Value.TotalItems);
            Assert.False(result.Value.HasNext);
            Assert.Equal(2, result.Value.PageNumber);
            Assert.Equal(10, result.Value.PageSize);
        }
    }
}
