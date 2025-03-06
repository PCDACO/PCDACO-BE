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
        public async Task Handle_NoSchedules_ReturnsNull()
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
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task Handle_WithSchedules_ReturnsAllTodaySchedules()
        {
            // Arrange
            var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
                _dbContext,
                "Consultant"
            );
            var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

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

            // Create multiple schedules for today
            var today = DateTimeOffset.UtcNow;
            var schedules = new List<InspectionSchedule>();
            for (int i = 0; i < 5; i++)
            {
                var schedule = new InspectionSchedule
                {
                    TechnicianId = technician.Id,
                    CarId = car.Id,
                    InspectionStatusId = pendingStatus.Id,
                    InspectionAddress = $"123 Main St {i + 1}",
                    InspectionDate = today,
                    CreatedBy = consultant.Id,
                };
                schedules.Add(schedule);
            }
            await _dbContext.InspectionSchedules.AddRangeAsync(schedules);

            // Create a schedule for tomorrow (should be excluded)
            var tomorrowSchedule = new InspectionSchedule
            {
                TechnicianId = technician.Id,
                CarId = car.Id,
                InspectionStatusId = pendingStatus.Id,
                InspectionAddress = "123 Future St",
                InspectionDate = today.AddDays(1),
                CreatedBy = consultant.Id,
            };
            await _dbContext.InspectionSchedules.AddAsync(tomorrowSchedule);

            await _dbContext.SaveChangesAsync();

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
            Assert.Equal(5, result.Value.Cars.Length);
            Assert.Equal("John Doe", result.Value.TechnicianName);
            Assert.Equal(today.Date, result.Value.InspectionDate.Date);

            // Verify car details
            var carDetail = result.Value.Cars.First();
            Assert.Equal(car.Id, carDetail.Id);
            Assert.Equal(model.Id, carDetail.ModelId);
            Assert.Equal(model.Name, carDetail.ModelName);
            Assert.Equal(manufacturer.Name, carDetail.ManufacturerName);
            Assert.Equal(2, carDetail.Images.Length);

            // Verify owner details
            Assert.Equal(owner.Id, carDetail.Owner.Id);
            Assert.Equal("Jane Smith", carDetail.Owner.Name);
            Assert.Equal("avatar.jpg", carDetail.Owner.AvatarUrl);

            // Verify addresses are included for all schedules
            Assert.Contains(result.Value.Cars, c => c.InspectionAddress == "123 Main St 1");
            Assert.Contains(result.Value.Cars, c => c.InspectionAddress == "123 Main St 5");
            Assert.DoesNotContain(result.Value.Cars, c => c.InspectionAddress == "123 Future St");
        }

        [Fact]
        public async Task Handle_NonPendingSchedules_AreExcluded()
        {
            // Arrange
            var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
                _dbContext,
                "Consultant"
            );
            var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

            var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
                _dbContext,
                "Technician"
            );
            var technician = await TestDataCreateUser.CreateTestUser(
                _dbContext,
                technicianRole,
                "tech@test.com",
                "John Doe",
                "0997778979",
                avatarUrl: "avatar.jpg"
            );
            _currentUser.SetUser(technician);

            // Create prerequisites
            var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
            var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
            var carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Pending");
            var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
            var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
            var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
                _dbContext,
                "Automatic"
            );
            var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

            // Create car
            var car = await TestDataCreateCar.CreateTestCarWithImages(
                _dbContext,
                owner.Id,
                model.Id,
                transmissionType,
                fuelType,
                carStatus,
                ["image.jpg"],
                aesEncryptionService: _aesEncryptionService,
                keyManagementService: _keyManagementService,
                encryptionSettings: _encryptionSettings
            );

            // Create statuses
            var pendingStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
                _dbContext,
                "Pending"
            );
            var approvedStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
                _dbContext,
                "Approved"
            );

            // Create schedules with different statuses
            var today = DateTimeOffset.UtcNow;

            var pendingSchedule = new InspectionSchedule
            {
                TechnicianId = technician.Id,
                CarId = car.Id,
                InspectionStatusId = pendingStatus.Id,
                InspectionAddress = "123 Pending St",
                InspectionDate = today,
                CreatedBy = consultant.Id,
            };

            var approvedSchedule = new InspectionSchedule
            {
                TechnicianId = technician.Id,
                CarId = car.Id,
                InspectionStatusId = approvedStatus.Id,
                InspectionAddress = "123 Approved St",
                InspectionDate = today,
                CreatedBy = consultant.Id,
            };

            await _dbContext.InspectionSchedules.AddRangeAsync([pendingSchedule, approvedSchedule]);
            await _dbContext.SaveChangesAsync();

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
            Assert.Single(result.Value.Cars);
            Assert.Equal("123 Pending St", result.Value.Cars.First().InspectionAddress);
        }
    }
}
