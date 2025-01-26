using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using UseCases.Abstractions;
using UseCases.UC_Car.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Car.Commands;

public class CreateCarTests : DatabaseTestBase
{
    private readonly GeometryFactory _geometryFactory;
    private readonly EncryptionSettings _encryptionSettings;
    private readonly IAesEncryptionService _aesService;
    private readonly IKeyManagementService _keyService;

    public CreateCarTests()
    {
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        _encryptionSettings = new EncryptionSettings { Key = TestConstants.MasterKey };
        _aesService = new AesEncryptionService();
        _keyService = new KeyManagementService();
    }

    private CreateCar.Query CreateValidCommand(
        TransmissionType transmissionType,
        FuelType fuelType,
        Guid? manufacturerId = null,
        Guid[]? amenityIds = null
    ) =>
        new(
            AmenityIds: amenityIds ?? [],
            ManufacturerId: manufacturerId ?? Guid.NewGuid(),
            LicensePlate: "ABC-12345",
            Color: "Red",
            Seat: 4,
            Description: "Test car",
            TransmissionTypeId: transmissionType.Id,
            FuelTypeId: fuelType.Id,
            FuelConsumption: 7.5m,
            RequiresCollateral: true,
            PricePerHour: 50m,
            PricePerDay: 500m,
            Latitude: 10.5m,
            Longtitude: 106.5m
        );

    [Fact]
    public async Task Handle_UserIsAdmin_ReturnsError()
    {
        // Arrange
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new CreateCar.Handler(
            _dbContext,
            _currentUser,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = CreateValidCommand(transmissionType: transmissionType, fuelType: fuelType);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này !", result.Errors);
    }

    [Fact]
    public async Task Handle_MissingAmenities_ReturnsError()
    {
        // Arrange
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var amenities = await TestDataCreateAmenity.CreateTestAmenities(_dbContext);

        var handler = new CreateCar.Handler(
            _dbContext,
            _currentUser,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = CreateValidCommand(
            transmissionType: transmissionType,
            fuelType: fuelType,
            manufacturerId: manufacturer.Id,
            amenityIds: [.. amenities.Select(a => a.Id)]
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Created, result.Status);

        // Verify database state
        var createdCar = await _dbContext
            .Cars.Include(c => c.CarAmenities)
            .Include(c => c.EncryptionKey)
            .FirstOrDefaultAsync(c => c.Id == result.Value.Id);

        Assert.NotNull(createdCar);

        // Verify encryption
        var decryptedLicense = await _aesService.Decrypt(
            createdCar.EncryptedLicensePlate,
            _keyService.DecryptKey(createdCar.EncryptionKey.EncryptedKey, _encryptionSettings.Key),
            createdCar.EncryptionKey.IV
        );

        Assert.Equal(command.LicensePlate, decryptedLicense);
    }

    [Fact]
    public async Task Handle_MissingManufacturer_ReturnsError()
    {
        // Arrange
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(user);

        // Use a random non-existent manufacturer ID
        var invalidManufacturerId = Guid.NewGuid();

        var handler = new CreateCar.Handler(
            _dbContext,
            _currentUser,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = CreateValidCommand(
            transmissionType: transmissionType,
            fuelType: fuelType,
            manufacturerId: invalidManufacturerId
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Hãng xe không tồn tại !", result.Errors);

        // Verify no car was created
        var carsCount = await _dbContext.Cars.CountAsync();
        Assert.Equal(0, carsCount);

        // Verify no encryption key was created for the car
        var encryptionKeysCount = await _dbContext.EncryptionKeys.CountAsync();
        Assert.Equal(1, encryptionKeysCount); // Only the user's key exists
    }

    [Fact]
    public async Task Validator_InvalidLicensePlate_ReturnsErrors()
    {
        // Arrange
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        var validator = new CreateCar.Validator();

        var invalidCommand = new CreateCar.Query(
            AmenityIds: [],
            ManufacturerId: Guid.Empty,
            LicensePlate: "SHORT",
            Color: "",
            Seat: 0,
            Description: new string('a', 501),
            TransmissionTypeId: transmissionType.Id,
            FuelTypeId: fuelType.Id,
            FuelConsumption: 0,
            RequiresCollateral: true,
            PricePerHour: 0,
            PricePerDay: 0,
            Latitude: null,
            Longtitude: null
        );

        // Act
        var result = validator.Validate(invalidCommand);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Phải chọn 1 hãng xe !", result.Errors.Select(e => e.ErrorMessage));
        Assert.Contains(
            "Biển số xe không được ít hơn 8 kí tự !",
            result.Errors.Select(e => e.ErrorMessage)
        );
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesCarSuccessfully()
    {
        // Arrange
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(user);
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var amenities = await TestDataCreateAmenity.CreateTestAmenities(_dbContext);

        var handler = new CreateCar.Handler(
            _dbContext,
            _currentUser,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = CreateValidCommand(
            transmissionType: transmissionType,
            fuelType: fuelType,
            manufacturerId: manufacturer.Id,
            amenityIds: [.. amenities.Select(a => a.Id)]
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Created, result.Status);

        // Verify car creation
        var createdCar = await _dbContext
            .Cars.Include(c => c.EncryptionKey)
            .Include(c => c.CarAmenities)
            .Include(c => c.CarStatistic)
            .FirstOrDefaultAsync(c => c.Id == result.Value.Id);

        Assert.NotNull(createdCar);
    }
}
