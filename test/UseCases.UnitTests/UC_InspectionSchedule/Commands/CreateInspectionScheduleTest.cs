using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_InspectionSchedule.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_InspectionSchedule.Commands;

[Collection("Test Collection")]
public class CreateInspectionScheduleTest(DatabaseTestBase fixture) : IAsyncLifetime
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

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: Guid.NewGuid(),
            CarId: Guid.NewGuid(),
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    // [Fact]
    // public async Task Handle_PendingStatusNotFound_ReturnsError()
    // {
    //     // Arrange
    //     var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
    //         _dbContext,
    //         "Consultant"
    //     );
    //     var user = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
    //     _currentUser.SetUser(user);
    //
    //     var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
    //     var command = new CreateInspectionSchedule.Command(
    //         TechnicianId: Guid.NewGuid(),
    //         CarId: Guid.NewGuid(),
    //         InspectionAddress: "123 Main St",
    //         InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
    //     );
    //
    //     // Act
    //     var result = await handler.Handle(command, CancellationToken.None);
    //
    //     // Assert
    //     Assert.Equal(ResultStatus.Error, result.Status);
    //     Assert.Contains(ResponseMessages.PendingStatusNotAvailable, result.Errors);
    // }

    [Fact]
    public async Task Handle_CarNotFound_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(user);

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: Guid.NewGuid(),
            CarId: Guid.NewGuid(),
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_CarNotInPendingStatus_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(user);

        // Create car with non-pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Available);

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: Guid.NewGuid(),
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarIsNotInPending, result.Errors);
    }

    [Fact]
    public async Task Handle_TechnicianIdNotFound_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(user);

        // Create car in pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: Guid.NewGuid(),
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.TechnicianNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_UserNotTechnician_ReturnsError()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create another non-technician user
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );

        // Create car in pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: driver.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.TechnicianNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesInspectionSchedule()
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
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create car in pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        var inspectionDate = DateTimeOffset.UtcNow.AddDays(1);
        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: inspectionDate
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify schedule was created
        var schedule = await _dbContext.InspectionSchedules.FirstOrDefaultAsync(s =>
            s.Id == result.Value.Id
        );

        Assert.NotNull(schedule);
        Assert.Equal(technician.Id, schedule.TechnicianId);
        Assert.Equal(car.Id, schedule.CarId);
        Assert.Equal(inspectionDate, schedule.InspectionDate);
        Assert.Equal("123 Main St", schedule.InspectionAddress);
        Assert.Equal(consultant.Id, schedule.CreatedBy);
    }

    private async Task<Car> CreateTestCar(Guid ownerId, CarStatusEnum status)
    {
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        return await TestDataCreateCar.CreateTestCar(
            _dbContext,
            ownerId,
            model.Id,
            transmissionType,
            fuelType,
            status
        );
    }
}