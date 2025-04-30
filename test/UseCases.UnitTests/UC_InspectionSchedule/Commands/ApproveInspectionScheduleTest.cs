using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_InspectionSchedule.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;

namespace UseCases.UnitTests.UC_InspectionSchedule.Commands;

[Collection("Test Collection")]
public class ApproveInspectionScheduleTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly AesEncryptionService _aesEncryptionService = fixture.AesEncryptionService;
    private readonly KeyManagementService _keyManagementService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

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
            _aesEncryptionService,
            _keyManagementService,
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
            _aesEncryptionService,
            _keyManagementService,
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
        var (schedule, _) = await SetupSignedSchedule(technician2);

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
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
    public async Task Handle_ScheduleNotSigned_ReturnsError()
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
            Status = InspectionScheduleStatusEnum.Pending,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
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
        Assert.Contains(
            "Chỉ có thể phê duyệt lịch kiểm định ở trạng thái đã ký hoặc đang xử lý",
            result.Errors
        );
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

        // Add gps device
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            OSBuildId = "OS12345",
            Status = DeviceStatusEnum.InUsed,
        };
        await _dbContext.GPSDevices.AddAsync(gpsDevice);
        await _dbContext.SaveChangesAsync();
        // Add car gps
        var carGPS = new CarGPS
        {
            CarId = car.Id,
            DeviceId = gpsDevice.Id,
            Location = new NetTopologySuite.Geometries.Point(106.6601, 10.7626) { SRID = 4326 },
        };
        await _dbContext.CarGPSes.AddAsync(carGPS);
        await _dbContext.SaveChangesAsync();

        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Signed,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddHours(-2),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
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

        // Add gps device
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            OSBuildId = "OS12345",
            Status = DeviceStatusEnum.InUsed,
        };
        await _dbContext.GPSDevices.AddAsync(gpsDevice);
        await _dbContext.SaveChangesAsync();
        // Add car gps
        var carGPS = new CarGPS
        {
            CarId = car.Id,
            DeviceId = gpsDevice.Id,
            Location = new NetTopologySuite.Geometries.Point(106.6601, 10.7626) { SRID = 4326 },
        };
        await _dbContext.CarGPSes.AddAsync(carGPS);
        await _dbContext.SaveChangesAsync();

        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Signed,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
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
    public async Task Handle_CarWithoutGPS_ReturnsError()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup car without GPS
        var (car, owner) = await SetupCarWithoutGPS();
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        // Create contract with signatures
        var contract = new CarContract
        {
            CarId = car.Id,
            Terms = "Test terms",
            InspectionResults = "Pending",
            Status = CarContractStatusEnum.OwnerSigned,
            OwnerSignature = "base64signature",
            OwnerSignatureDate = DateTimeOffset.UtcNow,
            TechnicianSignature = "base64signature",
            TechnicianSignatureDate = DateTimeOffset.UtcNow,
        };
        await _dbContext.CarContracts.AddAsync(contract);

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Signed,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
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
        Assert.Contains(
            "Xe chưa được gán thiết bị gps không thể duyệt lịch kiểm định",
            result.Errors
        );
    }

    [Fact]
    public async Task Handle_CarWithoutGPS_ButWithDeactivationReport_CanBeApproved()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup car without GPS
        var (car, owner) = await SetupCarWithoutGPS();

        // Create a consultant for creating reports
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        // Create a car report for deactivation
        var carReport = new CarReport
        {
            CarId = car.Id,
            Title = "Deactivation Request",
            Description = "Car needs to be deactivated",
            ReportType = CarReportType.DeactivateCar,
            Status = CarReportStatus.UnderReview,
            ReportedById = owner.Id,
            ResolvedById = consultant.Id,
        };
        await _dbContext.CarReports.AddAsync(carReport);
        await _dbContext.SaveChangesAsync();

        // Create contract with signatures
        var contract = await _dbContext.CarContracts.FirstOrDefaultAsync(c => c.CarId == car.Id);
        if (contract != null)
        {
            contract.OwnerSignatureDate = DateTimeOffset.UtcNow;
            contract.TechnicianSignatureDate = DateTimeOffset.UtcNow;
            contract.OwnerSignature = "base64signature";
            contract.TechnicianSignature = "base64signature";
            await _dbContext.SaveChangesAsync();
        }

        // Create inspection schedule with link to car report
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Signed,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
            CarReportId = carReport.Id,
            Type = InspectionScheduleType.ChangeGPS,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Approved deactivation request",
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
        Assert.Equal("Approved deactivation request", updatedSchedule.Note);
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

        // Setup car and schedule for testing
        var (schedule, contract) = await SetupSignedSchedule(technician);

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
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

        // Add gps device
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            OSBuildId = "OS12345",
            Status = DeviceStatusEnum.InUsed,
        };
        await _dbContext.GPSDevices.AddAsync(gpsDevice);
        await _dbContext.SaveChangesAsync();
        // Add car gps
        var carGPS = new CarGPS
        {
            CarId = car.Id,
            DeviceId = gpsDevice.Id,
            Location = new NetTopologySuite.Geometries.Point(106.6601, 10.7626) { SRID = 4326 },
        };
        await _dbContext.CarGPSes.AddAsync(carGPS);
        await _dbContext.SaveChangesAsync();

        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        // Get the contract but don't set owner signature date
        var contract = await _dbContext.CarContracts.FirstOrDefaultAsync(c => c.CarId == car.Id);
        contract!.TechnicianSignatureDate = DateTimeOffset.UtcNow; // Set technician signature only
        await _dbContext.SaveChangesAsync();

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Signed,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
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

        // Add gps device
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            OSBuildId = "OS12345",
            Status = DeviceStatusEnum.InUsed,
        };
        await _dbContext.GPSDevices.AddAsync(gpsDevice);
        await _dbContext.SaveChangesAsync();
        // Add car gps
        var carGPS = new CarGPS
        {
            CarId = car.Id,
            DeviceId = gpsDevice.Id,
            Location = new NetTopologySuite.Geometries.Point(106.6601, 10.7626) { SRID = 4326 },
        };
        await _dbContext.CarGPSes.AddAsync(carGPS);
        await _dbContext.SaveChangesAsync();

        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        // Get the contract but don't set technician signature date
        var contract = await _dbContext.CarContracts.FirstOrDefaultAsync(c => c.CarId == car.Id);
        contract!.OwnerSignatureDate = DateTimeOffset.UtcNow; // Set owner signature only
        await _dbContext.SaveChangesAsync();

        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = InspectionScheduleStatusEnum.Signed,
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
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

        // Setup car and schedule for testing
        var (schedule, contract) = await SetupSignedSchedule(technician);

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
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

    [Theory]
    [InlineData(InspectionScheduleStatusEnum.Approved)]
    [InlineData(InspectionScheduleStatusEnum.Rejected)]
    [InlineData(InspectionScheduleStatusEnum.Expired)]
    [InlineData(InspectionScheduleStatusEnum.Pending)]
    public async Task Handle_ApprovalWithInvalidStatus_ReturnsError(
        InspectionScheduleStatusEnum status
    )
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup car with GPS and contract
        var (car, owner) = await SetupCar();
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        // Add GPS to car
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            OSBuildId = "OS12345",
            Status = DeviceStatusEnum.InUsed,
        };
        await _dbContext.GPSDevices.AddAsync(gpsDevice);
        await _dbContext.SaveChangesAsync();

        var carGPS = new CarGPS
        {
            CarId = car.Id,
            DeviceId = gpsDevice.Id,
            Location = new NetTopologySuite.Geometries.Point(106.6601, 10.7626) { SRID = 4326 },
        };
        await _dbContext.CarGPSes.AddAsync(carGPS);
        await _dbContext.SaveChangesAsync();

        // Set up contract with signatures
        var contract = await _dbContext.CarContracts.FirstOrDefaultAsync(c => c.CarId == car.Id);
        contract!.OwnerSignatureDate = DateTimeOffset.UtcNow;
        contract.TechnicianSignatureDate = DateTimeOffset.UtcNow;
        contract.OwnerSignature = "base64signature";
        contract.TechnicianSignature = "base64signature";
        await _dbContext.SaveChangesAsync();

        // Create schedule with invalid status for approval
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = status, // Invalid status for approval
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
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
        Assert.Contains(
            "Chỉ có thể phê duyệt lịch kiểm định ở trạng thái đã ký hoặc đang xử lý",
            result.Errors
        );
    }

    [Theory]
    [InlineData(InspectionScheduleStatusEnum.Approved)]
    [InlineData(InspectionScheduleStatusEnum.Rejected)]
    [InlineData(InspectionScheduleStatusEnum.Expired)]
    public async Task Handle_RejectionWithInvalidStatus_ReturnsError(
        InspectionScheduleStatusEnum status
    )
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup car with contract
        var (car, owner) = await SetupCar();
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        // Create schedule with invalid status for rejection
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = status, // Invalid status for rejection
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Test note",
            IsApproved: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(
            "Chỉ có thể từ chối lịch kiểm định ở trạng thái chờ xử lý, đã ký hoặc đang xử lý",
            result.Errors
        );
    }

    [Theory]
    [InlineData(InspectionScheduleStatusEnum.Signed)]
    [InlineData(InspectionScheduleStatusEnum.InProgress)]
    public async Task Handle_ApprovalWithValidStatus_Succeeds(InspectionScheduleStatusEnum status)
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup car with GPS and contract
        var (car, owner) = await SetupCar();

        // Add GPS to car
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            OSBuildId = "OS12345",
            Status = DeviceStatusEnum.InUsed,
        };
        await _dbContext.GPSDevices.AddAsync(gpsDevice);
        await _dbContext.SaveChangesAsync();

        var carGPS = new CarGPS
        {
            CarId = car.Id,
            DeviceId = gpsDevice.Id,
            Location = new NetTopologySuite.Geometries.Point(106.6601, 10.7626) { SRID = 4326 },
        };
        await _dbContext.CarGPSes.AddAsync(carGPS);
        await _dbContext.SaveChangesAsync();

        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        // Set up contract with signatures
        var contract = await _dbContext.CarContracts.FirstOrDefaultAsync(c => c.CarId == car.Id);
        contract!.OwnerSignatureDate = DateTimeOffset.UtcNow;
        contract.TechnicianSignatureDate = DateTimeOffset.UtcNow;
        contract.OwnerSignature = "base64signature";
        contract.TechnicianSignature = "base64signature";
        await _dbContext.SaveChangesAsync();

        // Create schedule with valid status for approval
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = status, // Valid status for approval
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
            Type = InspectionScheduleType.NewCar,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Test approval with valid status",
            IsApproved: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains(ResponseMessages.Updated, result.SuccessMessage);

        // Verify schedule was approved
        var updatedSchedule = await _dbContext.InspectionSchedules.FindAsync(schedule.Id);
        Assert.NotNull(updatedSchedule);
        Assert.Equal(InspectionScheduleStatusEnum.Approved, updatedSchedule.Status);
    }

    [Theory]
    [InlineData(InspectionScheduleStatusEnum.Pending)]
    [InlineData(InspectionScheduleStatusEnum.Signed)]
    [InlineData(InspectionScheduleStatusEnum.InProgress)]
    public async Task Handle_RejectionWithValidStatus_Succeeds(InspectionScheduleStatusEnum status)
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Setup car with contract
        var (car, owner) = await SetupCar();
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        // Create schedule with valid status for rejection
        var schedule = new InspectionSchedule
        {
            TechnicianId = technician.Id,
            CarId = car.Id,
            Status = status, // Valid status for rejection
            InspectionAddress = "123 Main St",
            InspectionDate = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveInspectionSchedule.Handler(
            _dbContext,
            _currentUser,
            _aesEncryptionService,
            _keyManagementService,
            _encryptionSettings
        );
        var command = new ApproveInspectionSchedule.Command(
            Id: schedule.Id,
            Note: "Test rejection with valid status",
            IsApproved: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains(ResponseMessages.Updated, result.SuccessMessage);

        // Verify schedule was rejected
        var updatedSchedule = await _dbContext.InspectionSchedules.FindAsync(schedule.Id);
        Assert.NotNull(updatedSchedule);
        Assert.Equal(InspectionScheduleStatusEnum.Rejected, updatedSchedule.Status);
        Assert.Equal("Test rejection with valid status", updatedSchedule.Note);
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
    private async Task<(InspectionSchedule schedule, CarContract contract)> SetupSignedSchedule(
        User technician
    )
    {
        // Setup car with contract
        var (car, owner) = await SetupCar();
        // Add gps device
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            OSBuildId = "OS12345",
            Status = DeviceStatusEnum.InUsed,
        };
        await _dbContext.GPSDevices.AddAsync(gpsDevice);
        await _dbContext.SaveChangesAsync();
        // Add car gps
        var carGPS = new CarGPS
        {
            CarId = car.Id,
            DeviceId = gpsDevice.Id,
            Location = new NetTopologySuite.Geometries.Point(106.6601, 10.7626) { SRID = 4326 },
        };
        await _dbContext.CarGPSes.AddAsync(carGPS);
        await _dbContext.SaveChangesAsync();

        // Get the created contract
        var contract = await _dbContext.CarContracts.FirstOrDefaultAsync(c => c.CarId == car.Id);

        // Add signatures to the contract
        contract!.OwnerSignatureDate = DateTimeOffset.UtcNow.AddDays(-1);
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
            Status = InspectionScheduleStatusEnum.Signed,
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
        // Create owner with encrypted license number
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        // Generate encryption keys for owner
        (string ownerKey, string ownerIv) = await _keyManagementService.GenerateKeyAsync();
        string encryptedOwnerKey = _keyManagementService.EncryptKey(
            ownerKey,
            _encryptionSettings.Key
        );

        var ownerEncryptionKey = new EncryptionKey
        {
            EncryptedKey = encryptedOwnerKey,
            IV = ownerIv,
        };
        await _dbContext.EncryptionKeys.AddAsync(ownerEncryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create owner with encrypted phone and license
        string licenseNumber = "123456789";
        string encryptedLicense = await _aesEncryptionService.Encrypt(
            licenseNumber,
            ownerKey,
            ownerIv
        );
        string encryptedPhone = await _aesEncryptionService.Encrypt(
            "0987654321",
            ownerKey,
            ownerIv
        );

        var owner = new User
        {
            Name = "Test Owner",
            Email = "owner@test.com",
            Password = "password".HashString(),
            Address = "123 Owner St",
            RoleId = ownerRole.Id,
            EncryptionKeyId = ownerEncryptionKey.Id,
            EncryptedLicenseNumber = encryptedLicense,
            Phone = encryptedPhone,
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-30),
        };
        await _dbContext.Users.AddAsync(owner);
        await _dbContext.SaveChangesAsync();

        // Create car prerequisites
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Gasoline");

        // Create car with encrypted license plate
        (string carKey, string carIv) = await _keyManagementService.GenerateKeyAsync();
        string encryptedCarKey = _keyManagementService.EncryptKey(carKey, _encryptionSettings.Key);

        var carEncryptionKey = new EncryptionKey { EncryptedKey = encryptedCarKey, IV = carIv };
        await _dbContext.EncryptionKeys.AddAsync(carEncryptionKey);
        await _dbContext.SaveChangesAsync();

        string licensePlate = "51A-12345";
        string encryptedLicensePlate = await _aesEncryptionService.Encrypt(
            licensePlate,
            carKey,
            carIv
        );

        var car = new Car
        {
            OwnerId = owner.Id,
            ModelId = model.Id,
            TransmissionTypeId = transmissionType.Id,
            FuelTypeId = fuelType.Id,
            Color = "Black",
            LicensePlate = encryptedLicensePlate,
            Seat = 4,
            Description = "Test description",
            FuelConsumption = 7.5m,
            RequiresCollateral = true,
            Price = 500000,
            Terms = "Test terms",
            Status = CarStatusEnum.Pending,
            PickupLocation = new NetTopologySuite.Geometries.Point(106.6601, 10.7626)
            {
                SRID = 4326,
            },
            PickupAddress = "123 Pickup St",
        };

        await _dbContext.Cars.AddAsync(car);
        await _dbContext.SaveChangesAsync();

        // Create car statistic
        var carStatistic = new CarStatistic
        {
            CarId = car.Id,
            AverageRating = 0,
            TotalBooking = 0,
        };
        await _dbContext.CarStatistics.AddAsync(carStatistic);

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
        // Create car with encrypted data but without a contract
        var (car, owner) = await SetupCar();

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

    private async Task<(Car car, User owner)> SetupCarWithoutGPS()
    {
        // Create car with encrypted data but without a GPS device
        var (car, owner) = await SetupCar();

        // Remove the GPS association from the car
        var existingCarGPS = await _dbContext.CarGPSes.FirstOrDefaultAsync(c => c.CarId == car.Id);
        if (existingCarGPS != null)
        {
            _dbContext.CarGPSes.Remove(existingCarGPS);
            await _dbContext.SaveChangesAsync();
        }

        return (car, owner);
    }
    #endregion
}
