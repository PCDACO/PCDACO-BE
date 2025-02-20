using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_InspectionSchedule.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_InspectionSchedule.Commands;

[Collection("Test Collection")]
public class InProgressInspectionScheduleTest(DatabaseTestBase fixture) : IAsyncLifetime
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

        var handler = new InProgressInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new InProgressInspectionSchedule.Command(Id: Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_ScheduleNotFound_ReturnsError()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        var handler = new InProgressInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new InProgressInspectionSchedule.Command(Id: Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.InspectionScheduleNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_ScheduleNotPending_ReturnsError()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Create car
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Pending");
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var carModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            carModel.Id,
            transmissionType,
            fuelType,
            carStatus
        );

        // Create schedule with a status other than "pending"
        var approvedStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext,
            "Approved"
        );
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            InspectionStatusId = approvedStatus.Id,
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new InProgressInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new InProgressInspectionSchedule.Command(Id: schedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.OnlyUpdatePendingInspectionSchedule, result.Errors);
    }

    [Fact]
    public async Task Handle_InProgressStatusNotFound_ReturnsError()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Create car
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Pending");
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var carModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            carModel.Id,
            transmissionType,
            fuelType,
            carStatus
        );

        // Create schedule
        var pendingStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext,
            "Pending"
        );
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            InspectionStatusId = pendingStatus.Id,
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        // Remove in-progress status
        var inProgressStatus = await _dbContext.InspectionStatuses.FirstOrDefaultAsync(s =>
            s.Name == "InProgress"
        );
        if (inProgressStatus != null)
        {
            _dbContext.InspectionStatuses.Remove(inProgressStatus);
            await _dbContext.SaveChangesAsync();
        }

        var handler = new InProgressInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new InProgressInspectionSchedule.Command(Id: schedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.InProgressStatusNotAvailable, result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesSchedule()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Create car
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Pending");
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var carModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            carModel.Id,
            transmissionType,
            fuelType,
            carStatus
        );

        // Create schedule
        var pendingStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext,
            "Pending"
        );
        var inProgressStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext,
            "InProgress"
        );
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            InspectionStatusId = pendingStatus.Id,
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new InProgressInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new InProgressInspectionSchedule.Command(Id: schedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains(ResponseMessages.Updated, result.SuccessMessage);

        // Verify schedule was updated
        var updatedSchedule = await _dbContext.InspectionSchedules.FindAsync(schedule.Id);
        Assert.NotNull(updatedSchedule);
        Assert.Equal(inProgressStatus.Id, updatedSchedule.InspectionStatusId);
        Assert.NotNull(updatedSchedule.UpdatedAt);
    }

    [Fact]
    public void Validator_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new InProgressInspectionSchedule.Validator();
        var command = new InProgressInspectionSchedule.Command(Id: Guid.Empty);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Id");
    }
}
