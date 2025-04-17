using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_InspectionSchedule.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_InspectionSchedule.Commands;

[Collection("Test Collection")]
public class UpdateInspectionScheduleTest(DatabaseTestBase fixture) : IAsyncLifetime
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

        var handler = new UpdateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new UpdateInspectionSchedule.Command(
            Id: Guid.NewGuid(),
            TechnicianId: Guid.NewGuid(),
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

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
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        var handler = new UpdateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new UpdateInspectionSchedule.Command(
            Id: Guid.NewGuid(),
            TechnicianId: Guid.NewGuid(),
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.InspectionScheduleNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_ScheduleNotPendingOrInprogress_ReturnsError()
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
            Status = Domain.Enums.InspectionScheduleStatusEnum.Approved,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new UpdateInspectionSchedule.Command(
            Id: schedule.Id,
            TechnicianId: technician.Id,
            InspectionAddress: "123 Main St 1",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(2)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains(
            ResponseMessages.OnlyUpdatePendingOrInprogressInspectionSchedule,
            result.Errors
        );
    }

    [Fact]
    public async Task Handle_TechnicianNotFound_ReturnsError()
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
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new UpdateInspectionSchedule.Command(
            Id: schedule.Id,
            TechnicianId: Guid.NewGuid(), // Non-existent technician ID
            InspectionAddress: "123 Main St 1",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(2)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.TechnicianNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_TechnicianHasScheduleWithinOneHour_ReturnsError()
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

        // Create cars
        var car1 = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: carModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: Domain.Enums.CarStatusEnum.Pending
        );
        var car2 = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: carModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: Domain.Enums.CarStatusEnum.Pending
        );

        // Create a base time for testing
        var baseTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(10); // 10 AM tomorrow

        // Create an existing pending schedule
        var existingSchedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car1.Id,
            Status = Domain.Enums.InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "456 Existing St",
            InspectionDate = baseTime,
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(existingSchedule);

        // Create a pending schedule to update
        var pendingSchedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car2.Id,
            Status = Domain.Enums.InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "123 Pending St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(2),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(pendingSchedule);
        await _dbContext.SaveChangesAsync();

        // Try to update pending schedule to a time within one hour of existing schedule
        var requestedDate = baseTime.AddMinutes(45); // 45 minutes after existing schedule
        var handler = new UpdateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new UpdateInspectionSchedule.Command(
            Id: pendingSchedule.Id,
            TechnicianId: technician.Id,
            InspectionAddress: "123 Updated St",
            InspectionDate: requestedDate
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(
            ResponseMessages.TechnicianHasInspectionScheduleWithinOneHour,
            result.Errors
        );
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesSchedule()
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

        var originalSchedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = Domain.Enums.InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(originalSchedule);
        await _dbContext.SaveChangesAsync();

        var newInspectionDate = DateTimeOffset.UtcNow.AddDays(2);
        var handler = new UpdateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new UpdateInspectionSchedule.Command(
            Id: originalSchedule.Id,
            TechnicianId: technician.Id,
            InspectionAddress: "123 Main St 1",
            InspectionDate: newInspectionDate
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains(ResponseMessages.Updated, result.SuccessMessage);

        // Verify schedule was updated
        var updatedSchedule = await _dbContext.InspectionSchedules.FindAsync(originalSchedule.Id);
        Assert.NotNull(updatedSchedule);
        Assert.Equal(technician.Id, updatedSchedule.TechnicianId);
        Assert.Equal("123 Main St 1", updatedSchedule.InspectionAddress);
        Assert.Equal(newInspectionDate, updatedSchedule.InspectionDate);
        Assert.Equal(car.Id, updatedSchedule.CarId);
        Assert.NotNull(updatedSchedule.UpdatedAt);
    }

    [Fact]
    public void Validator_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new UpdateInspectionSchedule.Validator();
        var command = new UpdateInspectionSchedule.Command(
            Id: Guid.Empty,
            TechnicianId: Guid.Empty,
            InspectionAddress: string.Empty,
            InspectionDate: DateTimeOffset.UtcNow.AddDays(-1)
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Id");
        Assert.Contains(result.Errors, e => e.PropertyName == "TechnicianId");
        Assert.Contains(result.Errors, e => e.PropertyName == "InspectionAddress");
        Assert.Contains(result.Errors, e => e.PropertyName == "InspectionDate");
    }
}
