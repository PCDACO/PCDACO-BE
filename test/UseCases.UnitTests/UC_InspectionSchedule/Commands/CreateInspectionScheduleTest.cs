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

    [Fact]
    public async Task Handle_CarHasActiveSchedule_ReturnsError()
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

        // Create an existing active schedule for the car
        var existingSchedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Pending, // Active status
            InspectionAddress = "456 Existing St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(2),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(existingSchedule);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarHadInspectionSchedule, result.Errors);
    }

    [Fact]
    public async Task Handle_CarHasExpiredScheduleWithSameTechnician_ReturnsError()
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

        // Create an expired schedule for the car with the same technician
        var expiredSchedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Expired,
            InspectionAddress = "456 Expired St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(-2),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(expiredSchedule);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: DateTimeOffset.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(
            ResponseMessages.CarHadExpiredInspectionScheduleWithThisTechnician,
            result.Errors
        );
    }

    [Fact]
    public async Task Handle_TechnicianHasApprovedScheduleAfterRequestedTime_ReturnsError()
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
        var car1 = await CreateTestCar(owner.Id, CarStatusEnum.Pending);
        var car2 = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        // Create an existing approved schedule for the technician with a future date
        var futureDate = DateTimeOffset.UtcNow.AddDays(2); // 2 days in the future
        var existingSchedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car1.Id,
            Status = InspectionScheduleStatusEnum.Approved,
            InspectionAddress = "456 Existing St",
            InspectionDate = futureDate,
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(existingSchedule);
        await _dbContext.SaveChangesAsync();

        // Request a schedule for same technician before the approved schedule
        var requestedDate = DateTimeOffset.UtcNow.AddDays(1); // 1 day in the future
        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car2.Id,
            InspectionAddress: "123 Main St",
            InspectionDate: requestedDate
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.HasOverLapScheduleWithTheSameTechnician, result.Errors);
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

        // Create car in pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car1 = await CreateTestCar(owner.Id, CarStatusEnum.Pending);
        var car2 = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        // Create a base time for testing
        var baseTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(10); // 10 AM tomorrow

        // Create an existing pending schedule
        var existingSchedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car1.Id,
            Status = InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "456 Existing St",
            InspectionDate = baseTime,
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(existingSchedule);
        await _dbContext.SaveChangesAsync();

        // Request a schedule for same technician within one hour of existing schedule
        var requestedDate = baseTime.AddMinutes(45); // 45 minutes after existing schedule
        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician.Id,
            CarId: car2.Id,
            InspectionAddress: "123 Main St",
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
    public async Task Handle_CarHasRejectedSchedule_AllowsNewSchedule()
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

        // Create a rejected schedule for the car (can be same technician)
        var rejectedSchedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Rejected,
            InspectionAddress = "456 Rejected St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(rejectedSchedule);
        await _dbContext.SaveChangesAsync();

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
    }

    [Fact]
    public async Task Handle_CarHasExpiredScheduleWithDifferentTechnician_AllowsNewSchedule()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create two technicians
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician1 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech1@test.com"
        );
        var technician2 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech2@test.com"
        );

        // Create car in pending status
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );
        var car = await CreateTestCar(owner.Id, CarStatusEnum.Pending);

        // Create an expired schedule for the car with technician1
        var expiredSchedule = new InspectionSchedule
        {
            TechnicianId = technician1.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Expired,
            InspectionAddress = "456 Expired St",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(-2),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(expiredSchedule);
        await _dbContext.SaveChangesAsync();

        var inspectionDate = DateTimeOffset.UtcNow.AddDays(1);
        var handler = new CreateInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new CreateInspectionSchedule.Command(
            TechnicianId: technician2.Id, // Different technician
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
        Assert.Equal(technician2.Id, schedule.TechnicianId);
        Assert.Equal(car.Id, schedule.CarId);
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
