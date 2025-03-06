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
public class ApproveInspectionScheduleTest(DatabaseTestBase fixture) : IAsyncLifetime
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

        var handler = new ApproveInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new ApproveInspectionSchedule.Command(
            Id: Guid.NewGuid(),
            Note: "Test note",
            IsApproved: true
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
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        var handler = new ApproveInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new ApproveInspectionSchedule.Command(
            Id: Guid.NewGuid(),
            Note: "Test note",
            IsApproved: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.InspectionScheduleNotFound, result.Errors);
    }

    [Theory]
    [InlineData(true, ResponseMessages.ApproveStatusNotFound)]
    [InlineData(false, ResponseMessages.RejectStatusNotFound)]
    public async Task Handle_ApproveStatusNotFound_ReturnsError(
        bool isApproved,
        string expectedErrorMessage
    )
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
        var inProgressStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext,
            "Pending"
        );
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            InspectionStatusId = inProgressStatus.Id,
            InspectionAddress = "123 Main St 1",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Test note",
            IsApproved: isApproved
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(expectedErrorMessage, result.Errors);
    }

    [Fact]
    public async Task Handle_ScheduleNotInPending_ReturnsError()
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
        var pendingStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext,
            "InProgress"
        );
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            InspectionStatusId = pendingStatus.Id,
            InspectionAddress = "123 Main St 1",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Test note",
            IsApproved: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.OnlyUpdatePendingInspectionSchedule, result.Errors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_ValidRequest_UpdatesSchedule(bool isApproved)
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
        var inProgressStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext,
            "Pending"
        );
        var approvedStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext,
            "Approved"
        );
        var rejectedStatus = await TestDataCreateInspectionStatus.CreateTestInspectionStatus(
            _dbContext,
            "Rejected"
        );
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            InspectionStatusId = inProgressStatus.Id,
            InspectionAddress = "123 Main St 1",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(_dbContext, _currentUser);
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Test note",
            IsApproved: isApproved
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains(ResponseMessages.Updated, result.SuccessMessage);

        // Verify schedule was updated
        var updatedSchedule = await _dbContext.InspectionSchedules.FindAsync(schedule.Id);
        Assert.NotNull(updatedSchedule);
        Assert.Equal(
            isApproved ? approvedStatus.Id : rejectedStatus.Id,
            updatedSchedule.InspectionStatusId
        );
        Assert.Equal("Test note", updatedSchedule.Note);
        Assert.Equal("123 Main St 1", updatedSchedule.InspectionAddress);
        Assert.NotNull(updatedSchedule.UpdatedAt);
    }

    [Fact]
    public void Validator_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new ApproveInspectionSchedule.Validator();
        var command = new ApproveInspectionSchedule.Command(
            Id: Guid.Empty,
            Note: string.Empty,
            IsApproved: true
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Id");
        Assert.Contains(result.Errors, e => e.PropertyName == "Note");
    }
}
