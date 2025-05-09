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
public class DeleteInspectionScheduleTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotConsultant_ReturnsForbidden()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(user);

        var handler = new DeleteInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new DeleteInspectionSchedule.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_ScheduleNotFound_ReturnsNotFound()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        var handler = new DeleteInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new DeleteInspectionSchedule.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.InspectionScheduleNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_ScheduleNotPending_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);

        // Create owner and car prerequisites
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var carModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create car
        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: carModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: Domain.Enums.CarStatusEnum.Pending
        );

        // Create pending status and schedule for today (within 1 day)
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = Domain.Enums.InspectionScheduleStatusEnum.Approved,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddHours(20), // Less than 1 day away
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new DeleteInspectionSchedule.Command(schedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains(ResponseMessages.OnlyDeletePendingInspectionSchedule, result.Errors);
    }

    [Fact]
    public async Task Handle_ScheduleDateIsNow_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);

        // Create owner and car prerequisites
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var carModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create car
        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: carModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: Domain.Enums.CarStatusEnum.Pending
        );

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = Domain.Enums.InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow, // now
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new DeleteInspectionSchedule.Command(schedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CannotDeleteScheduleInProgressOrInThePast, result.Errors);
    }

    [Fact]
    public async Task Handle_ScheduleDateIsLessThanNow_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);

        // Create owner and car prerequisites
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var carModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create car
        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: carModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: Domain.Enums.CarStatusEnum.Pending
        );

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = Domain.Enums.InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(-1), // Less than now
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new DeleteInspectionSchedule.Command(schedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CannotDeleteScheduleInProgressOrInThePast, result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_DeletesScheduleSuccessfully()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create technician
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);

        // Create owner and car prerequisites
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var carModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create car
        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: carModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: Domain.Enums.CarStatusEnum.Pending
        );

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = Domain.Enums.InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(2), // More than 1 day away
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new DeleteInspectionSchedule.Command(schedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains(ResponseMessages.Deleted, result.SuccessMessage);

        // Verify schedule was soft deleted
        var deletedSchedule = await _dbContext
            .InspectionSchedules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == schedule.Id);

        Assert.NotNull(deletedSchedule);
        Assert.True(deletedSchedule.IsDeleted);
        Assert.NotNull(deletedSchedule.DeletedAt);

        // Verify it's not returned in normal queries
        var notFoundSchedule = await _dbContext
            .InspectionSchedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == schedule.Id && !s.IsDeleted);
        Assert.Null(notFoundSchedule);
    }
}
