using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_InspectionSchedule.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_InspectionSchedule.Queries;

[Collection("Test Collection")]
public class GetInDateScheduleForCurrentTechnicianTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotTechnician_ReturnsForbidden()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(user);

        var handler = new GetInDateScheduleForCurrentTechnician.Handler(_dbContext, _currentUser);
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

        var handler = new GetInDateScheduleForCurrentTechnician.Handler(_dbContext, _currentUser);
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
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
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
        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            carStatus
        );

        // Create pending status
        var pendingStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext
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

        var handler = new GetInDateScheduleForCurrentTechnician.Handler(_dbContext, _currentUser);
        var query = new GetInDateScheduleForCurrentTechnician.Query(PageNumber: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(10, result.Value.Items.Count());
        Assert.Equal(15, result.Value.TotalItems);
        Assert.True(result.Value.HasNext);
        Assert.Equal(1, result.Value.PageNumber);
        Assert.Equal(10, result.Value.PageSize);

        // Verify schedule data
        var firstSchedule = result.Value.Items.First();
        Assert.Equal(technician.Id, firstSchedule.TechnicianId);
        Assert.Equal(car.Id, firstSchedule.CarId);
        Assert.Equal(pendingStatus.Id, firstSchedule.InspectionStatusId);
        Assert.Equal("Pending", firstSchedule.StatusName);
        Assert.Equal(today.Date, firstSchedule.InspectionDate.Date);
    }

    [Fact]
    public async Task Handle_LastPage_ReturnsHasNextFalse()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
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
        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            carStatus
        );

        // Create pending status
        var pendingStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext
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

        var handler = new GetInDateScheduleForCurrentTechnician.Handler(_dbContext, _currentUser);
        var query = new GetInDateScheduleForCurrentTechnician.Query(PageNumber: 2, PageSize: 10);

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
