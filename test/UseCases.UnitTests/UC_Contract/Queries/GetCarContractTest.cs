using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Contract.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;

namespace UseCases.UnitTests.UC_Contract.Queries;

[Collection("Test Collection")]
public class GetCarContractTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly EncryptionSettings _encryptionSettings = new()
    {
        Key = TestConstants.MasterKey,
    };
    private readonly IAesEncryptionService _aesService = fixture.AesEncryptionService;
    private readonly IKeyManagementService _keyService = fixture.KeyManagementService;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ContractNotFound_ReturnsNotFound()
    {
        // Arrange
        var query = new GetCarContract.Query(Guid.NewGuid());
        var handler = new GetCarContract.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy hợp đồng.", result.Errors);
    }

    [Fact]
    public async Task Handle_TechnicianNotSigned_ReturnsErrorForOwner()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id, licenseNumber: "123456789012");
        _currentUser.SetUser(owner);

        var car = await CreateTestCarWithContract(owner.Id, technicianSigned: false);

        var query = new GetCarContract.Query(car.Id);
        var handler = new GetCarContract.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Vui lòng chờ kiểm định viên ký", result.Errors.First());
    }

    [Fact]
    public async Task Handle_ValidRequestAsTechnician_ReturnsSuccess()
    {
        // Arrange
        var techRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Technician");
        var technician = await CreateUserWithEncryptedData(
            techRole.Id,
            licenseNumber: "123456789012"
        );

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id, licenseNumber: "987654321012");

        _currentUser.SetUser(technician);

        var car = await CreateTestCarWithContract(
            owner.Id,
            technicianId: technician.Id,
            technicianSigned: true
        );
        var expectedLicensePlate = "TEST-LICENSE-123";

        var query = new GetCarContract.Query(car.Id);
        var handler = new GetCarContract.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Value);
        Assert.Contains(expectedLicensePlate, result.Value.HtmlContent);
        Assert.Contains("987654321012", result.Value.HtmlContent); // Owner license
    }

    [Fact]
    public async Task Handle_ValidRequestAsOwnerWithSignedContract_ReturnsSuccess()
    {
        // Arrange
        var techRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Technician");
        var technician = await CreateUserWithEncryptedData(
            techRole.Id,
            licenseNumber: "123456789012"
        );

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedData(ownerRole.Id, licenseNumber: "987654321012");

        _currentUser.SetUser(owner);

        var car = await CreateTestCarWithContract(
            owner.Id,
            technicianId: technician.Id,
            technicianSigned: true
        );
        var expectedLicensePlate = "TEST-LICENSE-123";

        var query = new GetCarContract.Query(car.Id);
        var handler = new GetCarContract.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Value);
        Assert.Contains(expectedLicensePlate, result.Value.HtmlContent);
        Assert.Contains("987654321012", result.Value.HtmlContent); // Owner license
    }

    #region Helper Methods

    private async Task<User> CreateUserWithEncryptedData(
        Guid roleId,
        string email = "test@example.com",
        string name = "Test User",
        string phoneNumber = "0123456789",
        string licenseNumber = null
    )
    {
        // Generate encryption key and encrypt data
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedPhoneNumber = await _aesService.Encrypt(phoneNumber, key, iv);
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        // Create encryption key
        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create user with encrypted data
        var user = new User
        {
            Name = name,
            Password = "password".HashString(),
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Email = email,
            RoleId = roleId,
            Phone = encryptedPhoneNumber,
            EncryptionKeyId = encryptionKey.Id,
        };

        // Add license if provided
        if (!string.IsNullOrEmpty(licenseNumber))
        {
            string encryptedLicenseNumber = await _aesService.Encrypt(licenseNumber, key, iv);
            user.EncryptedLicenseNumber = encryptedLicenseNumber;
            user.LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(1);
            user.LicenseImageFrontUrl = "front-url";
            user.LicenseImageBackUrl = "back-url";
            user.LicenseIsApproved = true;
            user.LicenseApprovedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    private async Task<Car> CreateTestCarWithContract(
        Guid ownerId,
        Guid? technicianId = null,
        bool technicianSigned = false
    )
    {
        // Create prerequisites
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Generate encryption key and encrypt license plate
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string licensePlate = "TEST-LICENSE-123";
        string encryptedLicensePlate = await _aesService.Encrypt(licensePlate, key, iv);
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        // Create encryption key
        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create car
        var car = new Car
        {
            OwnerId = ownerId,
            ModelId = model.Id,
            EncryptionKeyId = encryptionKey.Id,
            EncryptedLicensePlate = encryptedLicensePlate,
            FuelTypeId = fuelType.Id,
            TransmissionTypeId = transmission.Id,
            Status = CarStatusEnum.Available,
            Color = "Red",
            Seat = 4,
            Description = "Test car description",
            FuelConsumption = 7.5m,
            Price = 100m,
            RequiresCollateral = false,
            Terms = "Test terms",
            PickupAddress = "Test address",
            PickupLocation = new NetTopologySuite.Geometries.Point(0, 0) { SRID = 4326 }, // Replace with actual coordinates
        };

        await _dbContext.Cars.AddAsync(car);
        await _dbContext.SaveChangesAsync();

        // Create car statistic
        var carStatistic = new CarStatistic
        {
            CarId = car.Id,
            TotalBooking = 0,
            AverageRating = 0,
        };

        await _dbContext.CarStatistics.AddAsync(carStatistic);
        await _dbContext.SaveChangesAsync();

        // Create GPS device
        var gpsDevice = new GPSDevice
        {
            Name = "Test GPS Device",
            OSBuildId = "OS12345",
            Status = DeviceStatusEnum.InUsed,
        };

        await _dbContext.GPSDevices.AddAsync(gpsDevice);
        await _dbContext.SaveChangesAsync();

        // Create contract
        var contract = new CarContract
        {
            CarId = car.Id,
            TechnicianId = technicianId,
            Status = CarContractStatusEnum.Pending,
            Terms = "Test contract terms",
            TechnicianSignatureDate = technicianSigned ? DateTimeOffset.UtcNow : null,
            InspectionResults = "All systems operational",
            GPSDeviceId = gpsDevice.Id,
        };

        await _dbContext.CarContracts.AddAsync(contract);
        await _dbContext.SaveChangesAsync();

        return car;
    }

    #endregion
}
