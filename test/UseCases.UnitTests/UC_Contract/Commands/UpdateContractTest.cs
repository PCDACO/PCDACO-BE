using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Contract.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;

namespace UseCases.UnitTests.UC_Contract.Commands;

[Collection("Test Collection")]
public class UpdateContractTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly IAesEncryptionService _aesService = fixture.AesEncryptionService;
    private readonly IKeyManagementService _keyService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ValidRequest_UpdatesContractSuccessfully()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await CreateUserWithEncryptedData(technicianRole.Id);
        _currentUser.SetUser(technician);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id);

        var car = await CreateTestCar(owner.Id);

        // Create GPS device
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            Status = DeviceStatusEnum.Available,
        };
        await _dbContext.GPSDevices.AddAsync(gpsDevice);
        await _dbContext.SaveChangesAsync();

        // Associate GPS with car
        var carGps = new CarGPS
        {
            CarId = car.Id,
            DeviceId = gpsDevice.Id,
            Location = _geometryFactory.CreatePoint(new Coordinate(106.7, 10.8)),
        };
        await _dbContext.CarGPSes.AddAsync(carGps);
        await _dbContext.SaveChangesAsync();

        // Create inspection schedule
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await CreateUserWithEncryptedData(consultantRole.Id);

        var inspectionSchedule = new InspectionSchedule
        {
            CarId = car.Id,
            TechnicianId = technician.Id,
            Status = InspectionScheduleStatusEnum.InProgress,
            InspectionDate = DateTimeOffset.UtcNow,
            InspectionAddress = "Test Address",
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(inspectionSchedule);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateContract.Handler(_dbContext, _currentUser);
        var command = new UpdateContract.Command(inspectionSchedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Cập nhật hợp đồng thành công", result.SuccessMessage);

        // Verify contract was created/updated
        var contract = await _dbContext.CarContracts.FindAsync(result.Value.ContractId);
        Assert.NotNull(contract);
        Assert.Equal(car.Id, contract.CarId);
        Assert.Equal(technician.Id, contract.TechnicianId);
        Assert.Equal(gpsDevice.Id, contract.GPSDeviceId);
        Assert.Equal(CarContractStatusEnum.Pending, contract.Status);
    }

    [Fact]
    public async Task Handle_UserNotTechnician_ReturnsForbidden()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id);
        _currentUser.SetUser(owner);

        var handler = new UpdateContract.Handler(_dbContext, _currentUser);
        var command = new UpdateContract.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(ResponseMessages.ForbiddenAudit, result.Errors.First());
    }

    [Fact]
    public async Task Handle_ScheduleNotFound_ReturnsNotFound()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await CreateUserWithEncryptedData(technicianRole.Id);
        _currentUser.SetUser(technician);

        var handler = new UpdateContract.Handler(_dbContext, _currentUser);
        var command = new UpdateContract.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("Không tìm thấy lịch kiểm định", result.Errors.First());
    }

    [Fact]
    public async Task Handle_ScheduleNotInProgress_ReturnsConflict()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await CreateUserWithEncryptedData(technicianRole.Id);
        _currentUser.SetUser(technician);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id);
        var car = await CreateTestCar(owner.Id);

        // Create GPS device and associate with car
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            Status = DeviceStatusEnum.Available,
        };
        await _dbContext.GPSDevices.AddAsync(gpsDevice);

        var carGps = new CarGPS
        {
            CarId = car.Id,
            DeviceId = gpsDevice.Id,
            Location = _geometryFactory.CreatePoint(new Coordinate(106.7, 10.8)),
        };
        await _dbContext.CarGPSes.AddAsync(carGps);

        // Create inspection schedule with Pending status
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await CreateUserWithEncryptedData(consultantRole.Id);

        var inspectionSchedule = new InspectionSchedule
        {
            CarId = car.Id,
            TechnicianId = technician.Id,
            Status = InspectionScheduleStatusEnum.Pending, // Not InProgress
            InspectionDate = DateTimeOffset.UtcNow,
            InspectionAddress = "Test Address",
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(inspectionSchedule);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateContract.Handler(_dbContext, _currentUser);
        var command = new UpdateContract.Command(inspectionSchedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("Lịch kiểm định không ở trạng thái đang diễn ra", result.Errors.First());
    }

    [Fact]
    public async Task Handle_WrongTechnician_ReturnsForbidden()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician1 = await CreateUserWithEncryptedData(technicianRole.Id, "tech1@example.com");
        var technician2 = await CreateUserWithEncryptedData(technicianRole.Id, "tech2@example.com");
        _currentUser.SetUser(technician1);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id);
        var car = await CreateTestCar(owner.Id);

        // Create GPS device and associate with car
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            Status = DeviceStatusEnum.Available,
        };
        await _dbContext.GPSDevices.AddAsync(gpsDevice);

        var carGps = new CarGPS
        {
            CarId = car.Id,
            DeviceId = gpsDevice.Id,
            Location = _geometryFactory.CreatePoint(new Coordinate(106.7, 10.8)),
        };
        await _dbContext.CarGPSes.AddAsync(carGps);

        // Create inspection schedule assigned to technician2
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await CreateUserWithEncryptedData(consultantRole.Id);

        var inspectionSchedule = new InspectionSchedule
        {
            CarId = car.Id,
            TechnicianId = technician2.Id, // Different technician
            Status = InspectionScheduleStatusEnum.InProgress,
            InspectionDate = DateTimeOffset.UtcNow,
            InspectionAddress = "Test Address",
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(inspectionSchedule);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateContract.Handler(_dbContext, _currentUser);
        var command = new UpdateContract.Command(inspectionSchedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("Bạn không phải là kiểm định viên được chỉ định", result.Errors.First());
    }

    [Fact]
    public async Task Handle_NoGPSDevice_ReturnsError()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await CreateUserWithEncryptedData(technicianRole.Id);
        _currentUser.SetUser(technician);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id);
        var car = await CreateTestCar(owner.Id);

        // Create inspection schedule without GPS device
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await CreateUserWithEncryptedData(consultantRole.Id);

        var inspectionSchedule = new InspectionSchedule
        {
            CarId = car.Id,
            TechnicianId = technician.Id,
            Status = InspectionScheduleStatusEnum.InProgress,
            InspectionDate = DateTimeOffset.UtcNow,
            InspectionAddress = "Test Address",
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(inspectionSchedule);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateContract.Handler(_dbContext, _currentUser);
        var command = new UpdateContract.Command(inspectionSchedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Xe chưa được gán thiết bị GPS", result.Errors.First());
    }

    [Fact]
    public async Task Handle_ExistingContract_UpdatesExistingContract()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await CreateUserWithEncryptedData(technicianRole.Id);
        _currentUser.SetUser(technician);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id);
        var car = await CreateTestCar(owner.Id);

        // Create GPS device and associate with car
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            Status = DeviceStatusEnum.Available,
        };
        await _dbContext.GPSDevices.AddAsync(gpsDevice);

        var carGps = new CarGPS
        {
            CarId = car.Id,
            DeviceId = gpsDevice.Id,
            Location = _geometryFactory.CreatePoint(new Coordinate(106.7, 10.8)),
        };
        await _dbContext.CarGPSes.AddAsync(carGps);

        // Create existing contract
        var existingContract = new CarContract
        {
            CarId = car.Id,
            Status = CarContractStatusEnum.Pending,
            Terms = "Previous terms",
        };
        await _dbContext.CarContracts.AddAsync(existingContract);

        // Create inspection schedule
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await CreateUserWithEncryptedData(consultantRole.Id);

        var inspectionSchedule = new InspectionSchedule
        {
            CarId = car.Id,
            TechnicianId = technician.Id,
            Status = InspectionScheduleStatusEnum.InProgress,
            InspectionDate = DateTimeOffset.UtcNow,
            InspectionAddress = "Test Address",
            CreatedBy = consultant.Id,
        };
        await _dbContext.InspectionSchedules.AddAsync(inspectionSchedule);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateContract.Handler(_dbContext, _currentUser);
        var command = new UpdateContract.Command(inspectionSchedule.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify contract was updated
        var updatedContract = await _dbContext.CarContracts.FindAsync(existingContract.Id);
        Assert.NotNull(updatedContract);
        Assert.Equal(car.Id, updatedContract.CarId);
        Assert.Equal(technician.Id, updatedContract.TechnicianId);
        Assert.Equal(gpsDevice.Id, updatedContract.GPSDeviceId);
        Assert.Equal(CarContractStatusEnum.Pending, updatedContract.Status);
        Assert.Equal("Previous terms", updatedContract.Terms); // Terms should be preserved
    }

    #region Helper Methods

    private async Task<User> CreateUserWithEncryptedData(
        Guid roleId,
        string email = "test@example.com",
        string name = "Test User",
        string phoneNumber = "0123456789",
        string licenseNumber = "123456789012"
    )
    {
        // Generate encryption key and encrypt data
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedPhoneNumber = await _aesService.Encrypt(phoneNumber, key, iv);
        string encryptedLicenseNumber = await _aesService.Encrypt(licenseNumber, key, iv);
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        // Create encryption key
        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create user with encrypted data
        var user = new User
        {
            Name = name,
            Email = email,
            Password = "password".HashString(),
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            RoleId = roleId,
            Phone = encryptedPhoneNumber,
            EncryptionKeyId = encryptionKey.Id,
            EncryptedLicenseNumber = encryptedLicenseNumber,
            LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(1),
            LicenseImageFrontUrl = "front-url",
            LicenseImageBackUrl = "back-url",
            LicenseIsApproved = true,
            LicenseApprovedAt = DateTimeOffset.UtcNow,
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    private async Task<Car> CreateTestCar(Guid ownerId, string licensePlate = "ABC-12345")
    {
        // Create prerequisites
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Gasoline");

        // Create pickup location point
        var pickupLocation = _geometryFactory.CreatePoint(new Coordinate(106.7004238, 10.7756587));
        pickupLocation.SRID = 4326;

        // Create car
        var car = new Car
        {
            OwnerId = ownerId,
            ModelId = model.Id,
            LicensePlate = licensePlate,
            FuelTypeId = fuelType.Id,
            TransmissionTypeId = transmission.Id,
            Status = CarStatusEnum.Available,
            Color = "Red",
            Seat = 4,
            Description = "Test car",
            FuelConsumption = 7.5m,
            Price = 100m,
            PickupLocation = pickupLocation,
            PickupAddress = "Test address",
            Terms = "Standard terms",
        };

        await _dbContext.Cars.AddAsync(car);
        await _dbContext.SaveChangesAsync();

        return car;
    }

    #endregion
}
