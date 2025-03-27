using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Car.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Car.Commands;

[Collection("Test Collection")]
public class UpdateCarTest(DatabaseTestBase fixture) : IAsyncLifetime
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
    public async Task Handle_ValidRequest_UpdatesCarSuccessfully()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var newModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        // Create amenities
        var amenities = await TestDataCreateAmenity.CreateTestAmenities(_dbContext);

        // Create car
        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        // Add initial amenities
        await AddAmenityToCar(car.Id, amenities[0].Id);

        // Create handler
        var handler = new UpdateCar.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _encryptionSettings,
            _keyService,
            _geometryFactory
        );

        // Updated details
        string newColor = "Midnight Blue";
        string newLicensePlate = "ABC-54321";
        int newSeat = 5;
        string newDescription = "Updated description";
        decimal newFuelConsumption = 6.2m;
        bool newRequiresCollateral = true;
        decimal newPrice = 250.99m;
        string newPickupAddress = "123 New Address St";
        decimal newPickupLatitude = 10.8756587m;
        decimal newPickupLongitude = 106.8004238m;

        var command = new UpdateCar.Commamnd(
            CarId: car.Id,
            AmenityIds: new[] { amenities[1].Id, amenities[2].Id }, // Change amenities
            ModelId: newModel.Id,
            TransmissionTypeId: transmissionType.Id,
            FuelTypeId: fuelType.Id,
            LicensePlate: newLicensePlate,
            Color: newColor,
            Seat: newSeat,
            Description: newDescription,
            FuelConsumption: newFuelConsumption,
            RequiresCollateral: newRequiresCollateral,
            Price: newPrice,
            PickupLatitude: newPickupLatitude,
            PickupLongitude: newPickupLongitude,
            PickupAddress: newPickupAddress
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify car was updated
        var updatedCar = await _dbContext
            .Cars.Include(c => c.EncryptionKey)
            .Include(c => c.CarAmenities)
            .FirstOrDefaultAsync(c => c.Id == car.Id);

        Assert.NotNull(updatedCar);
        Assert.Equal(newModel.Id, updatedCar.ModelId);
        Assert.Equal(newColor, updatedCar.Color);
        Assert.Equal(newSeat, updatedCar.Seat);
        Assert.Equal(newDescription, updatedCar.Description);
        Assert.Equal(newFuelConsumption, updatedCar.FuelConsumption);
        Assert.Equal(newRequiresCollateral, updatedCar.RequiresCollateral);
        Assert.Equal(newPrice, updatedCar.Price);
        Assert.Equal(newPickupAddress, updatedCar.PickupAddress);

        // Verify license plate was encrypted correctly
        string decryptedKey = _keyService.DecryptKey(
            updatedCar.EncryptionKey.EncryptedKey,
            _encryptionSettings.Key
        );
        string decryptedLicensePlate = await _aesService.Decrypt(
            updatedCar.EncryptedLicensePlate,
            decryptedKey,
            updatedCar.EncryptionKey.IV
        );
        Assert.Equal(newLicensePlate, decryptedLicensePlate);

        // Verify amenities were updated
        Assert.Equal(3, updatedCar.CarAmenities.Count);
        Assert.Contains(updatedCar.CarAmenities, ca => ca.AmenityId == amenities[1].Id);
        Assert.Contains(updatedCar.CarAmenities, ca => ca.AmenityId == amenities[2].Id);
        Assert.Contains(updatedCar.CarAmenities, ca => ca.AmenityId == amenities[0].Id);

        // Verify pickup location was updated
        Assert.Equal((double)newPickupLongitude, updatedCar.PickupLocation.X, 6);
        Assert.Equal((double)newPickupLatitude, updatedCar.PickupLocation.Y, 6);
    }

    [Fact]
    public async Task Handle_UserIsAdmin_ReturnsForbidden()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(admin);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        // Create car (with a different owner)
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com"
        );

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var handler = new UpdateCar.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _encryptionSettings,
            _keyService,
            _geometryFactory
        );

        var command = new UpdateCar.Commamnd(
            CarId: car.Id,
            AmenityIds: Array.Empty<Guid>(),
            ModelId: model.Id,
            TransmissionTypeId: transmissionType.Id,
            FuelTypeId: fuelType.Id,
            LicensePlate: "ABC-12345",
            Color: "Red",
            Seat: 4,
            Description: "Description",
            FuelConsumption: 7.5m,
            RequiresCollateral: false,
            Price: 100.0m,
            PickupLatitude: 10.7756587m,
            PickupLongitude: 106.7004238m,
            PickupAddress: "Test Address"
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
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var handler = new UpdateCar.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _encryptionSettings,
            _keyService,
            _geometryFactory
        );

        var command = new UpdateCar.Commamnd(
            CarId: Guid.NewGuid(), // Non-existent car ID
            AmenityIds: Array.Empty<Guid>(),
            ModelId: model.Id,
            TransmissionTypeId: transmissionType.Id,
            FuelTypeId: fuelType.Id,
            LicensePlate: "ABC-12345",
            Color: "Red",
            Seat: 4,
            Description: "Description",
            FuelConsumption: 7.5m,
            RequiresCollateral: false,
            Price: 100.0m,
            PickupLatitude: 10.7756587m,
            PickupLongitude: 106.7004238m,
            PickupAddress: "Test Address"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_AmenitiesNotFound_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var handler = new UpdateCar.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _encryptionSettings,
            _keyService,
            _geometryFactory
        );

        var command = new UpdateCar.Commamnd(
            CarId: car.Id,
            AmenityIds: new[] { Guid.NewGuid() }, // Non-existent amenity ID
            ModelId: model.Id,
            TransmissionTypeId: transmissionType.Id,
            FuelTypeId: fuelType.Id,
            LicensePlate: "ABC-12345",
            Color: "Red",
            Seat: 4,
            Description: "Description",
            FuelConsumption: 7.5m,
            RequiresCollateral: false,
            Price: 100.0m,
            PickupLatitude: 10.7756587m,
            PickupLongitude: 106.7004238m,
            PickupAddress: "Test Address"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.AmenitiesNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_TransmissionTypeNotFound_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var handler = new UpdateCar.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _encryptionSettings,
            _keyService,
            _geometryFactory
        );

        var command = new UpdateCar.Commamnd(
            CarId: car.Id,
            AmenityIds: Array.Empty<Guid>(),
            ModelId: model.Id,
            TransmissionTypeId: Guid.NewGuid(), // Non-existent transmission type ID
            FuelTypeId: fuelType.Id,
            LicensePlate: "ABC-12345",
            Color: "Red",
            Seat: 4,
            Description: "Description",
            FuelConsumption: 7.5m,
            RequiresCollateral: false,
            Price: 100.0m,
            PickupLatitude: 10.7756587m,
            PickupLongitude: 106.7004238m,
            PickupAddress: "Test Address"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.TransmissionTypeNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_FuelTypeNotFound_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var handler = new UpdateCar.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _encryptionSettings,
            _keyService,
            _geometryFactory
        );

        var command = new UpdateCar.Commamnd(
            CarId: car.Id,
            AmenityIds: Array.Empty<Guid>(),
            ModelId: model.Id,
            TransmissionTypeId: transmissionType.Id,
            FuelTypeId: Guid.NewGuid(), // Non-existent fuel type ID
            LicensePlate: "ABC-12345",
            Color: "Red",
            Seat: 4,
            Description: "Description",
            FuelConsumption: 7.5m,
            RequiresCollateral: false,
            Price: 100.0m,
            PickupLatitude: 10.7756587m,
            PickupLongitude: 106.7004238m,
            PickupAddress: "Test Address"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.FuelTypeNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_ModelNotFound_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var handler = new UpdateCar.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _encryptionSettings,
            _keyService,
            _geometryFactory
        );

        var command = new UpdateCar.Commamnd(
            CarId: car.Id,
            AmenityIds: Array.Empty<Guid>(),
            ModelId: Guid.NewGuid(), // Non-existent model ID
            TransmissionTypeId: transmissionType.Id,
            FuelTypeId: fuelType.Id,
            LicensePlate: "ABC-12345",
            Color: "Red",
            Seat: 4,
            Description: "Description",
            FuelConsumption: 7.5m,
            RequiresCollateral: false,
            Price: 100.0m,
            PickupLatitude: 10.7756587m,
            PickupLongitude: 106.7004238m,
            PickupAddress: "Test Address"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.ModelNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_UpdatesTimestamp_WhenCarUpdated()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        // Record original timestamp
        var originalTimestamp = car.UpdatedAt;

        // Wait a moment to ensure timestamps are different
        await Task.Delay(10);

        var handler = new UpdateCar.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _encryptionSettings,
            _keyService,
            _geometryFactory
        );

        var command = new UpdateCar.Commamnd(
            CarId: car.Id,
            AmenityIds: Array.Empty<Guid>(),
            ModelId: model.Id,
            TransmissionTypeId: transmissionType.Id,
            FuelTypeId: fuelType.Id,
            LicensePlate: "ABC-12345",
            Color: "Blue", // Updated color
            Seat: 4,
            Description: "Description",
            FuelConsumption: 7.5m,
            RequiresCollateral: false,
            Price: 100.0m,
            PickupLatitude: 10.7756587m,
            PickupLongitude: 106.7004238m,
            PickupAddress: "Test Address"
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedCar = await _dbContext.Cars.Where(c => c.Id == car.Id).SingleOrDefaultAsync();

        Assert.NotNull(updatedCar);
        Assert.NotEqual(originalTimestamp, updatedCar.UpdatedAt);
    }

    #region Helper Methods

    private async Task AddAmenityToCar(Guid carId, Guid amenityId)
    {
        var carAmenity = new CarAmenity { CarId = carId, AmenityId = amenityId };

        await _dbContext.CarAmenities.AddAsync(carAmenity);
        await _dbContext.SaveChangesAsync();
    }

    #endregion
}
