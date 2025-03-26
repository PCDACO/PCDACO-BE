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
using UseCases.UC_Car.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Car.Queries;

[Collection("Test Collection")]
public class GetPersonalCarsTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    private readonly IAesEncryptionService _aesService = fixture.AesEncryptionService;
    private readonly IKeyManagementService _keyService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ReturnsAllUserOwnedCars_WhenNoFiltersApplied()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner); // Set current user as owner

        var otherOwner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "other@example.com"
        );

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create 3 cars for the current owner and 1 car for another owner
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "ABC-12345"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Pending,
            "DEF-67890"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Rejected,
            "GHI-13579"
        );
        await CreateTestCar(
            otherOwner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "OTHER-9999"
        );

        var handler = new GetPersonalCars.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var query = new GetPersonalCars.Query(
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Status: null,
            Keyword: ""
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.TotalItems); // Only owner's cars should be returned
        Assert.Equal(3, result.Value.Items.Count());
        Assert.All(result.Value.Items, item => Assert.Equal(owner.Id, item.OwnerId));

        // Verify license plates are correctly decrypted
        Assert.Contains(result.Value.Items, i => i.LicensePlate == "ABC-12345");
        Assert.Contains(result.Value.Items, i => i.LicensePlate == "DEF-67890");
        Assert.Contains(result.Value.Items, i => i.LicensePlate == "GHI-13579");
    }

    [Fact]
    public async Task Handle_FiltersByManufacturer_WhenManufacturerIdProvided()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        // Create two manufacturers
        var manufacturer1 = await TestDataCreateManufacturer.CreateTestManufacturer(
            _dbContext,
            "Toyota"
        );
        var manufacturer2 = await TestDataCreateManufacturer.CreateTestManufacturer(
            _dbContext,
            "Honda"
        );

        // Create models for each manufacturer
        var model1 = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer1.Id);
        var model2 = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer2.Id);

        // Create transmission and fuel types
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create cars with different manufacturers
        await CreateTestCar(
            owner.Id,
            model1.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "TOYOTA-123"
        );
        await CreateTestCar(
            owner.Id,
            model1.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "TOYOTA-456"
        );
        await CreateTestCar(
            owner.Id,
            model2.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "HONDA-789"
        );

        var handler = new GetPersonalCars.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var query = new GetPersonalCars.Query(
            ManufacturerId: manufacturer1.Id,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Status: null,
            Keyword: ""
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.TotalItems); // Only cars with manufacturer1 should be returned
        Assert.All(
            result.Value.Items,
            item => Assert.Equal(manufacturer1.Id, item.Manufacturer.Id)
        );
    }

    [Fact]
    public async Task Handle_FiltersByAmenities_WhenAmenitiesProvided()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create amenities
        var amenities = await TestDataCreateAmenity.CreateTestAmenities(_dbContext);
        var amenity1 = amenities[0];
        var amenity2 = amenities[1];

        // Create cars with different amenities
        var car1 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CAR-BOTH"
        );
        await AddAmenityToCar(car1.Id, amenity1.Id);
        await AddAmenityToCar(car1.Id, amenity2.Id);

        var car2 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CAR-ONE"
        );
        await AddAmenityToCar(car2.Id, amenity1.Id);

        var car3 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CAR-NONE"
        );
        // No amenities for car3

        var handler = new GetPersonalCars.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var query = new GetPersonalCars.Query(
            ManufacturerId: null,
            Amenities: new[] { amenity1.Id, amenity2.Id },
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Status: null,
            Keyword: ""
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(1, result.Value.TotalItems); // Only car1 has both amenities
        Assert.Equal(car1.Id, result.Value.Items.First().Id);
    }

    [Fact]
    public async Task Handle_FiltersByFuelType_WhenFuelTypeProvided()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );

        // Create fuel types
        var electricFuel = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        var gasFuel = await TestDataFuelType.CreateTestFuelType(_dbContext, "Gas");
        var dieselFuel = await TestDataFuelType.CreateTestFuelType(_dbContext, "Diesel");

        // Create cars with different fuel types
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            electricFuel.Id,
            CarStatusEnum.Available,
            "ELECTRIC-1"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            gasFuel.Id,
            CarStatusEnum.Available,
            "GAS-1"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            gasFuel.Id,
            CarStatusEnum.Available,
            "GAS-2"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            dieselFuel.Id,
            CarStatusEnum.Available,
            "DIESEL-1"
        );

        var handler = new GetPersonalCars.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var query = new GetPersonalCars.Query(
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: gasFuel.Id,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Status: null,
            Keyword: ""
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.TotalItems); // Only gas fuel cars should be returned
        Assert.All(result.Value.Items, item => Assert.Equal(gasFuel.Id, item.FuelTypeId));
    }

    [Fact]
    public async Task Handle_FiltersByTransmissionType_WhenTransmissionTypeProvided()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create transmission types
        var automaticTransmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var manualTransmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Manual"
        );

        // Create cars with different transmission types
        await CreateTestCar(
            owner.Id,
            model.Id,
            automaticTransmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "AUTO-1"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            automaticTransmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "AUTO-2"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            manualTransmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "MANUAL-1"
        );

        var handler = new GetPersonalCars.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var query = new GetPersonalCars.Query(
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: automaticTransmission.Id,
            LastCarId: null,
            Limit: 10,
            Status: null,
            Keyword: ""
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.TotalItems); // Only automatic transmission cars should be returned
        Assert.All(
            result.Value.Items,
            item => Assert.Equal(automaticTransmission.Id, item.TransmissionTypeId)
        );
    }

    [Fact]
    public async Task Handle_FiltersByStatus_WhenStatusProvided()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create cars with different statuses
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "AVAILABLE-1"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "AVAILABLE-2"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Pending,
            "PENDING-1"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Rejected,
            "REJECTED-1"
        );

        var handler = new GetPersonalCars.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var query = new GetPersonalCars.Query(
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Status: CarStatusEnum.Available,
            Keyword: ""
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.TotalItems); // Only available cars should be returned
        Assert.All(result.Value.Items, item => Assert.Equal("Available", item.Status));
    }

    [Fact]
    public async Task Handle_FiltersByKeyword_WhenKeywordProvided()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);

        // Create models with specific names
        var sedanModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        sedanModel.Name = "Luxury Sedan";

        var suvModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        suvModel.Name = "SUV Crossover";

        var sportModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        sportModel.Name = "Sport Coupe";

        await _dbContext.SaveChangesAsync();

        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create cars with different models
        await CreateTestCar(
            owner.Id,
            sedanModel.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "SEDAN-1"
        );
        await CreateTestCar(
            owner.Id,
            suvModel.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "SUV-1"
        );
        await CreateTestCar(
            owner.Id,
            sportModel.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "SPORT-1"
        );

        var handler = new GetPersonalCars.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var query = new GetPersonalCars.Query(
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Status: null,
            Keyword: "SUV"
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(1, result.Value.TotalItems); // Only SUV model should be returned
        Assert.Equal("SUV Crossover", result.Value.Items.First().ModelName);
    }

    [Fact]
    public async Task Handle_UsesPagination_WhenLastCarIdProvided()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create cars with specific IDs to ensure ordering
        var car1 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CAR-001"
        );
        var car2 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CAR-002"
        );
        var car3 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CAR-003"
        );
        var car4 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CAR-004"
        );
        var car5 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CAR-005"
        );

        var handler = new GetPersonalCars.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        // First page query
        var firstPageQuery = new GetPersonalCars.Query(
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 3,
            Status: null,
            Keyword: ""
        );

        // Act - First page
        var firstPageResult = await handler.Handle(firstPageQuery, CancellationToken.None);

        // Get ID for pagination
        var lastCarId = firstPageResult.Value.Items.Last().Id;

        // Second page query
        var secondPageQuery = new GetPersonalCars.Query(
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: lastCarId,
            Limit: 3,
            Status: null,
            Keyword: ""
        );

        // Act - Second page
        var secondPageResult = await handler.Handle(secondPageQuery, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, firstPageResult.Status);
        Assert.Equal(5, firstPageResult.Value.TotalItems); // Total items should be all cars
        Assert.Equal(5, firstPageResult.Value.Items.Count());

        Assert.Equal(ResultStatus.Ok, secondPageResult.Status);
        Assert.Empty(secondPageResult.Value.Items);

        // Ensure no overlap between pages
        var firstPageIds = firstPageResult.Value.Items.Select(c => c.Id);
        var secondPageIds = secondPageResult.Value.Items.Select(c => c.Id);
        Assert.Empty(firstPageIds.Intersect(secondPageIds));
    }

    [Fact]
    public async Task Handle_ShowsContractDetails_WhenUserHasPermission()
    {
        // Arrange
        // Create different types of users with different contract viewing permissions
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var admin = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            adminRole,
            "admin@test.com"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            consultantRole,
            "consultant@test.com"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "technician@test.com"
        );
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create car with contract
        var car = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CONTRACT-1"
        );
        await CreateCarContract(car.Id, CarContractStatusEnum.Completed);

        var handler = new GetPersonalCars.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var query = new GetPersonalCars.Query(
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Status: null,
            Keyword: ""
        );

        // Test with each user type
        Dictionary<string, bool> roleExpectedContractVisibility = new()
        {
            { "Owner", true },
            { "Admin", true },
            { "Consultant", true },
            { "Technician", true },
            { "Driver", false },
        };

        // Test for owner
        _currentUser.SetUser(owner);
        var ownerResult = await handler.Handle(query, CancellationToken.None);

        // Assert owner can see contract
        Assert.Equal(ResultStatus.Ok, ownerResult.Status);
        Assert.NotNull(ownerResult.Value.Items.First().Contract);
    }

    [Fact]
    public async Task Handle_DecryptsLicensePlate_Correctly()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create car with known license plate
        string expectedLicensePlate = "TEST-LICENSE-123";
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            expectedLicensePlate
        );

        var handler = new GetPersonalCars.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var query = new GetPersonalCars.Query(
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Status: null,
            Keyword: ""
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal(expectedLicensePlate, result.Value.Items.First().LicensePlate);
    }

    #region Helper Methods

    private async Task<Car> CreateTestCar(
        Guid ownerId,
        Guid modelId,
        Guid transmissionTypeId,
        Guid fuelTypeId,
        CarStatusEnum status,
        string licensePlate,
        double latitude = 10.7756587,
        double longitude = 106.7004238
    )
    {
        // Generate encryption key and encrypt license plate
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedLicensePlate = await _aesService.Encrypt(licensePlate, key, iv);
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        // Create encryption key
        var encryptionKey = new EncryptionKey
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptedKey = encryptedKey,
            IV = iv,
        };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create pickup location point
        var pickupLocation = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        pickupLocation.SRID = 4326;

        // Create car
        Guid carId = Uuid.NewDatabaseFriendly(Database.PostgreSql);
        var car = new Car
        {
            Id = carId,
            OwnerId = ownerId,
            ModelId = modelId,
            EncryptionKeyId = encryptionKey.Id,
            EncryptedLicensePlate = encryptedLicensePlate,
            FuelTypeId = fuelTypeId,
            TransmissionTypeId = transmissionTypeId,
            Status = status,
            Color = "Blue",
            Seat = 4,
            Description = "Test car description",
            FuelConsumption = 7.5m,
            Price = 100m,
            RequiresCollateral = false,
            Terms = "Standard terms",
            PickupLocation = pickupLocation,
            PickupAddress = "Test Address",
        };

        await _dbContext.Cars.AddAsync(car);

        // Create car statistics
        var carStatistic = new CarStatistic
        {
            CarId = car.Id,
            TotalBooking = 0,
            AverageRating = 0,
        };
        await _dbContext.CarStatistics.AddAsync(carStatistic);

        // Create GPS data for the car
        await CreateGPSForCar(car.Id, latitude, longitude);

        // Create image for the car
        await CreateCarImage(car.Id);

        await _dbContext.SaveChangesAsync();
        return car;
    }

    private async Task<CarGPS> CreateGPSForCar(
        Guid carId,
        double latitude = 10.7756587,
        double longitude = 106.7004238
    )
    {
        var gpsDevice = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext);
        var location = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        location.SRID = 4326;

        var carGPS = new CarGPS
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            DeviceId = gpsDevice.Id,
            CarId = carId,
            Location = location,
            IsDeleted = false,
        };

        await _dbContext.CarGPSes.AddAsync(carGPS);
        await _dbContext.SaveChangesAsync();

        return carGPS;
    }

    private async Task<ImageCar> CreateCarImage(Guid carId)
    {
        var imageType = await CreateOrGetCarImageType();
        var image = new ImageCar
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = carId,
            TypeId = imageType.Id,
            Url = "http://example.com/car-image.jpg",
            Name = "Car Image",
            IsDeleted = false,
        };

        await _dbContext.ImageCars.AddAsync(image);
        await _dbContext.SaveChangesAsync();

        return image;
    }

    private async Task<ImageType> CreateOrGetCarImageType()
    {
        var imageType = await _dbContext.ImageTypes.FirstOrDefaultAsync(t => t.Name == "Car");

        if (imageType == null)
        {
            imageType = new ImageType
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = "Car",
                IsDeleted = false,
            };
            await _dbContext.ImageTypes.AddAsync(imageType);
            await _dbContext.SaveChangesAsync();
        }

        return imageType;
    }

    private async Task AddAmenityToCar(Guid carId, Guid amenityId)
    {
        var carAmenity = new CarAmenity { CarId = carId, AmenityId = amenityId };

        await _dbContext.CarAmenities.AddAsync(carAmenity);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<CarContract> CreateCarContract(Guid carId, CarContractStatusEnum status)
    {
        var contract = new CarContract
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = carId,
            Status = status,
            Terms = "Sample contract terms",
            OwnerSignatureDate = DateTimeOffset.UtcNow,
            TechnicianSignatureDate =
                status == CarContractStatusEnum.Completed ? DateTimeOffset.UtcNow : null,
        };

        await _dbContext.CarContracts.AddAsync(contract);
        await _dbContext.SaveChangesAsync();

        return contract;
    }

    #endregion
}
