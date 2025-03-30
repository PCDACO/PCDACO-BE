using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Moq;
using Persistance.Data;
using UseCases.Abstractions;
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
    private readonly Mock<IAesEncryptionService> _aesEncryptionService = new();
    private readonly Mock<IKeyManagementService> _keyManagementService = new();
    private readonly EncryptionSettings _encryptionSettings = new() { Key = "TestKey" };

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotTechnician_ReturnsForbidden()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(user);

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService.Object,
            _keyManagementService.Object,
            _encryptionSettings
        );
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

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService.Object,
            _keyManagementService.Object,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: Guid.NewGuid(),
            Note: "Test note",
            IsApproved: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.InspectionScheduleNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_TechnicianNotAssigned_ReturnsForbidden()
    {
        // Arrange
        // Create technician users
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
        _currentUser.SetUser(technician1);

        // Setup car and schedule assigned to technician2
        var (schedule, _) = await SetupInProgressSchedule(technician2);

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService.Object,
            _keyManagementService.Object,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Test note",
            IsApproved: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không phải là kiểm định viên được chỉ định", result.Errors);
    }

    [Fact]
    public async Task Handle_ScheduleNotInProgress_ReturnsError()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup schedule with Pending status
        var (car, owner) = await SetupCar();
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Pending, // Not InProgress
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService.Object,
            _keyManagementService.Object,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Test note",
            IsApproved: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.OnlyUpdateInProgressInspectionSchedule, result.Errors);
    }

    [Fact]
    public async Task Handle_ScheduleExpired_ReturnsError()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup schedule with inspection date more than 1 hour in the past
        var (car, owner) = await SetupCar();
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.InProgress,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddHours(-2), // 2 hours in the past
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService.Object,
            _keyManagementService.Object,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Test note",
            IsApproved: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.InspectionScheduleExpired, result.Errors);
    }

    [Fact]
    public async Task Handle_ContractNotFound_ReturnsNotFound()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup car without contract
        var (car, owner) = await SetupCarWithoutContract();
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.InProgress,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService.Object,
            _keyManagementService.Object,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Test note",
            IsApproved: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy hợp đồng xe", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidApproval_UpdatesScheduleAndContractAndCar()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup mock encryption services
        SetupMockEncryptionServices();

        // Setup car and schedule for testing
        var (schedule, contract) = await SetupInProgressSchedule(technician);

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService.Object,
            _keyManagementService.Object,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Approved with good condition",
            IsApproved: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains(ResponseMessages.Updated, result.SuccessMessage);

        // Verify schedule was updated
        var updatedSchedule = await _dbContext.InspectionSchedules.FindAsync(schedule.Id);
        Assert.NotNull(updatedSchedule);
        Assert.Equal(InspectionScheduleStatusEnum.Approved, updatedSchedule.Status);
        Assert.Equal("Approved with good condition", updatedSchedule.Note);
        Assert.NotNull(updatedSchedule.UpdatedAt);

        // Verify contract was updated
        var updatedContract = await _dbContext.CarContracts.FindAsync(contract.Id);
        Assert.NotNull(updatedContract);
        Assert.Equal("Đã duyệt", updatedContract.InspectionResults);
        Assert.Equal(CarContractStatusEnum.Completed, updatedContract.Status);
        Assert.NotNull(updatedContract.Terms);
        Assert.NotNull(updatedContract.UpdatedAt);
    }

    [Fact]
    public async Task Handle_ContractNotSignedByOwner_ReturnsError()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup car and schedule
        var (car, owner) = await SetupCar();
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        // Get the contract but don't set owner signature date
        var contract = await _dbContext.CarContracts.FirstOrDefaultAsync(c => c.CarId == car.Id);
        contract.TechnicianSignatureDate = DateTimeOffset.UtcNow; // Set technician signature only
        await _dbContext.SaveChangesAsync();

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.InProgress,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService.Object,
            _keyManagementService.Object,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Test note",
            IsApproved: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Hợp đồng chưa được ký bởi chủ xe", result.Errors);
    }

    [Fact]
    public async Task Handle_ContractNotSignedByTechnician_ReturnsError()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup car and schedule
        var (car, owner) = await SetupCar();
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        // Get the contract but don't set technician signature date
        var contract = await _dbContext.CarContracts.FirstOrDefaultAsync(c => c.CarId == car.Id);
        contract.OwnerSignatureDate = DateTimeOffset.UtcNow; // Set owner signature only
        await _dbContext.SaveChangesAsync();

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.InProgress,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService.Object,
            _keyManagementService.Object,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Test note",
            IsApproved: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Hợp đồng chưa được ký bởi kiểm định viên", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRejection_UpdatesScheduleAndContractAndCar()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup mock encryption services
        SetupMockEncryptionServices();

        // Setup car and schedule for testing
        var (schedule, contract) = await SetupInProgressSchedule(technician);

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService.Object,
            _keyManagementService.Object,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Rejected due to maintenance issues",
            IsApproved: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains(ResponseMessages.Updated, result.SuccessMessage);

        // Verify schedule was updated
        var updatedSchedule = await _dbContext.InspectionSchedules.FindAsync(schedule.Id);
        Assert.NotNull(updatedSchedule);
        Assert.Equal(InspectionScheduleStatusEnum.Rejected, updatedSchedule.Status);
        Assert.Equal("Rejected due to maintenance issues", updatedSchedule.Note);
        Assert.NotNull(updatedSchedule.UpdatedAt);
    }

    [Fact]
    public void Validator_EmptyId_ReturnsValidationError()
    {
        // Arrange
        var validator = new ApproveInspectionSchedule.Validator();
        var command = new ApproveInspectionSchedule.Command(
            Id: Guid.Empty,
            Note: "Test note",
            IsApproved: true
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        // Arrange
        var validator = new ApproveInspectionSchedule.Validator();
        var command = new ApproveInspectionSchedule.Command(
            Id: Guid.NewGuid(),
            Note: "Test note",
            IsApproved: true
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    #region Helper Methods
    private async Task<(InspectionSchedule schedule, CarContract contract)> SetupInProgressSchedule(
        User technician
    )
    {
        // Setup car with contract
        var (car, owner) = await SetupCar();

        // Get the created contract
        var contract = await _dbContext.CarContracts.FirstOrDefaultAsync(c => c.CarId == car.Id);

        // Add signatures to the contract
        contract.OwnerSignatureDate = DateTimeOffset.UtcNow.AddDays(-1);
        contract.TechnicianSignatureDate = DateTimeOffset.UtcNow.AddDays(-1);
        await _dbContext.SaveChangesAsync();

        // Create consultant for creating schedule
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        // Create schedule
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.InProgress,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };

        // Add a photo to the inspection schedule for template generation
        var photo = new InspectionPhoto
        {
            ScheduleId = schedule.Id,
            PhotoUrl = "http://example.com/photo.jpg",
            Type = InspectionPhotoType.ExteriorCar,
        };
        schedule.Photos.Add(photo);

        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        return (schedule, contract);
    }

    private async Task<(Car car, User owner)> SetupCar()
    {
        // Create owner
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        // Create car prerequisites
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Gasoline");

        // Create car
        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Pending
        );

        // Create GPS device for the car
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            OSBuildId = "OS12345",
            Status = DeviceStatusEnum.Available,
        };
        await _dbContext.GPSDevices.AddAsync(gpsDevice);
        await _dbContext.SaveChangesAsync();

        // Create contract for car
        var contract = new CarContract
        {
            CarId = car.Id,
            GPSDeviceId = gpsDevice.Id,
            Terms = "Test terms",
            InspectionResults = "Pending",
            Status = CarContractStatusEnum.Pending,
        };
        await _dbContext.CarContracts.AddAsync(contract);
        await _dbContext.SaveChangesAsync();

        return (car, owner);
    }

    private async Task<(Car car, User owner)> SetupCarWithoutContract()
    {
        // Create owner
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        // Create car prerequisites
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Gasoline");

        // Create car without a contract
        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Pending
        );

        // Explicitly remove any auto-generated contract
        var existingContract = await _dbContext.CarContracts.FirstOrDefaultAsync(c =>
            c.CarId == car.Id
        );
        if (existingContract != null)
        {
            _dbContext.CarContracts.Remove(existingContract);
            await _dbContext.SaveChangesAsync();
        }

        return (car, owner);
    }

    private void SetupMockEncryptionServices()
    {
        // Setup mock for AesEncryptionService
        _aesEncryptionService
            .Setup(x => x.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("DecryptedValue");

        // Setup mock for KeyManagementService
        _keyManagementService
            .Setup(x => x.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("DecryptedKey");
    }
    #endregion
}
