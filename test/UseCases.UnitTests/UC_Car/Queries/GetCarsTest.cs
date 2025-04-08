using System.Text;
using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Infrastructure.Encryption;
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
public class GetCarsTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
    private readonly IAesEncryptionService _aesService = fixture.AesEncryptionService;
    private readonly IKeyManagementService _keyService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ReturnsAllAvailableCars_WhenNoFiltersApplied()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create 3 available cars and 1 pending car
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
            CarStatusEnum.Available,
            "XYZ-67890"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "DEF-24680"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Pending,
            "GHI-13579"
        );

        var handler = new GetCars.Handler(
            _dbContext,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Keyword: string.Empty,
            StartTime: null,
            EndTime: null
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.TotalItems); // Only available cars should be returned
        Assert.Equal(3, result.Value.Items.Count());
    }

    [Fact]
    public async Task Handle_FiltersByManufacturer_WhenManufacturerIdProvided()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

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
            "ABC-12345"
        );
        await CreateTestCar(
            owner.Id,
            model1.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "XYZ-67890"
        );
        await CreateTestCar(
            owner.Id,
            model2.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "DEF-24680"
        );

        var handler = new GetCars.Handler(
            _dbContext,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: manufacturer1.Id,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Keyword: string.Empty,
            StartTime: null,
            EndTime: null
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
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

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
            "ABC-12345"
        );
        await AddAmenityToCar(car1.Id, amenity1.Id);
        await AddAmenityToCar(car1.Id, amenity2.Id);

        var car2 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "XYZ-67890"
        );
        await AddAmenityToCar(car2.Id, amenity1.Id);

        var car3 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "DEF-24680"
        );
        // No amenities for car3

        var handler = new GetCars.Handler(
            _dbContext,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: new[] { amenity1.Id, amenity2.Id },
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Keyword: string.Empty,
            StartTime: null,
            EndTime: null
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(1, result.Value.TotalItems); // Only car1 has both amenities
        Assert.Equal(car1.Id, result.Value.Items.First().Id);
    }

    [Fact]
    public async Task Handle_FiltersByTransmissionAndFuelType_WhenTypesProvided()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        // Create transmission types
        var automaticTransmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var manualTransmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Manual"
        );

        // Create fuel types
        var electricFuel = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        var gasFuel = await TestDataFuelType.CreateTestFuelType(_dbContext, "Gas");

        // Create cars with different combinations
        await CreateTestCar(
            owner.Id,
            model.Id,
            automaticTransmission.Id,
            electricFuel.Id,
            CarStatusEnum.Available,
            "ABC-12345"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            automaticTransmission.Id,
            gasFuel.Id,
            CarStatusEnum.Available,
            "XYZ-67890"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            manualTransmission.Id,
            electricFuel.Id,
            CarStatusEnum.Available,
            "DEF-24680"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            manualTransmission.Id,
            gasFuel.Id,
            CarStatusEnum.Available,
            "GHI-13579"
        );

        var handler = new GetCars.Handler(
            _dbContext,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: electricFuel.Id,
            TransmissionTypes: automaticTransmission.Id,
            LastCarId: null,
            Limit: 10,
            Keyword: string.Empty,
            StartTime: null,
            EndTime: null
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(1, result.Value.TotalItems); // Only one car matches both criteria
        var car = result.Value.Items.First();
        Assert.Equal(electricFuel.Id, car.FuelTypeId);
        Assert.Equal(automaticTransmission.Id, car.TransmissionId);
    }

    [Fact]
    public async Task Handle_FiltersByKeyword_WhenKeywordProvided()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);

        // Create models with specific names
        var sedanModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        sedanModel.Name = "Sedan Model";

        var suvModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        suvModel.Name = "SUV Model";

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
            "ABC-12345"
        );
        await CreateTestCar(
            owner.Id,
            suvModel.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "XYZ-67890"
        );
        await CreateTestCar(
            owner.Id,
            sportModel.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "DEF-24680"
        );

        var handler = new GetCars.Handler(
            _dbContext,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Keyword: "SUV",
            StartTime: null,
            EndTime: null
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(1, result.Value.TotalItems); // Only SUV model should be returned
        Assert.Equal("SUV Model", result.Value.Items.First().ModelName);
    }

    [Fact]
    public async Task Handle_FiltersByLocation_WhenLocationAndRadiusProvided()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Center coordinates (Ho Chi Minh City center)
        double centerLatitude = 10.7756587;
        double centerLongitude = 106.7004238;

        // Create cars at different locations
        // Car 1: Very close to center (within 1km)
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "ABC-12345",
            centerLatitude + 0.001,
            centerLongitude + 0.001
        ); // ~150m away

        // Car 2: A bit further (within 5km)
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "XYZ-67890",
            centerLatitude + 0.02,
            centerLongitude + 0.02
        ); // ~3km away

        // Car 3: Very far (more than 10km)
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "DEF-24680",
            centerLatitude + 0.1,
            centerLongitude + 0.1
        ); // ~15km away

        var handler = new GetCars.Handler(
            _dbContext,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetCars.Query(
            Latitude: (decimal)centerLatitude,
            Longtitude: (decimal)centerLongitude,
            Radius: 5000, // 5km radius
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Keyword: string.Empty,
            StartTime: null,
            EndTime: null
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.TotalItems); // Only the first 2 cars should be within 5km
    }

    [Fact]
    public async Task Handle_FiltersByDateRange_WhenStartAndEndTimeProvided()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
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

        // Create 3 cars
        var car1 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "ABC-12345"
        );
        var car2 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "XYZ-67890"
        );
        var car3 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "DEF-24680"
        );

        // Create bookings
        // Car 1: Booked for next week
        var nextWeekStart = DateTime.UtcNow.AddDays(7);
        var nextWeekEnd = nextWeekStart.AddDays(3);
        await CreateBooking(
            driver.Id,
            car1.Id,
            BookingStatusEnum.Approved,
            nextWeekStart,
            nextWeekEnd
        );

        // Car 2: Booked for tomorrow
        var tomorrowStart = DateTime.UtcNow.AddDays(1);
        var tomorrowEnd = tomorrowStart.AddDays(2);
        await CreateBooking(
            driver.Id,
            car2.Id,
            BookingStatusEnum.Approved,
            tomorrowStart,
            tomorrowEnd
        );

        // Car 3: No bookings

        var handler = new GetCars.Handler(
            _dbContext,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        // Query for cars available in 2 days (should exclude car 2)
        var queryDate = DateTime.UtcNow.AddDays(2);
        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Keyword: string.Empty,
            StartTime: queryDate,
            EndTime: queryDate.AddDays(1)
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.TotalItems); // Car 1 and Car 3 should be available
        // Car 2 should be excluded (marked as rented) due to booking conflict
        Assert.DoesNotContain(
            result.Value.Items,
            item => item.Id == car2.Id && item.Status == "Available"
        );
    }

    [Fact]
    public async Task Handle_UsesPagination_WhenLastCarIdProvided()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

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
            "CAR-00001"
        );
        var car2 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CAR-00002"
        );
        var car3 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CAR-00003"
        );
        var car4 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CAR-00004"
        );
        var car5 = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "CAR-00005"
        );

        var handler = new GetCars.Handler(
            _dbContext,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        // First page query (2 items)
        var firstPageQuery = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 2,
            Keyword: string.Empty,
            StartTime: null,
            EndTime: null
        );

        // Act - First page
        var firstPageResult = await handler.Handle(firstPageQuery, CancellationToken.None);
        var lastCarId = firstPageResult.Value.Items.Last().Id;

        // Second page query (use last car ID from first page)
        var secondPageQuery = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: lastCarId,
            Limit: 2,
            Keyword: string.Empty,
            StartTime: null,
            EndTime: null
        );

        // Act - Second page
        var secondPageResult = await handler.Handle(secondPageQuery, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, firstPageResult.Status);
        Assert.Equal(2, firstPageResult.Value.Items.Count());

        Assert.Equal(ResultStatus.Ok, secondPageResult.Status);
        Assert.Equal(2, secondPageResult.Value.Items.Count());

        // Ensure no overlap between pages
        var firstPageIds = firstPageResult.Value.Items.Select(c => c.Id);
        var secondPageIds = secondPageResult.Value.Items.Select(c => c.Id);
        Assert.Empty(firstPageIds.Intersect(secondPageIds));
    }

    [Fact]
    public async Task Handle_DecryptsLicensePlate_ForEachCar()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create a car with known license plate
        string plainLicensePlate = "TEST-9999";
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            plainLicensePlate
        );

        var handler = new GetCars.Handler(
            _dbContext,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            LastCarId: null,
            Limit: 10,
            Keyword: string.Empty,
            StartTime: null,
            EndTime: null
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal(plainLicensePlate, result.Value.Items.First().LicensePlate);
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
            LicensePlate = licensePlate,
            FuelTypeId = fuelTypeId,
            TransmissionTypeId = transmissionTypeId,
            Status = status,
            Color = "Red",
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
        var gpsDevice = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext);
        var location = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        location.SRID = 4326;

        var carGPS = new CarGPS
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            DeviceId = gpsDevice.Id,
            CarId = car.Id,
            Location = location,
            IsDeleted = false,
        };

        await _dbContext.CarGPSes.AddAsync(carGPS);
        await _dbContext.SaveChangesAsync();

        return car;
    }

    private async Task AddAmenityToCar(Guid carId, Guid amenityId)
    {
        var carAmenity = new CarAmenity { CarId = carId, AmenityId = amenityId };

        await _dbContext.CarAmenities.AddAsync(carAmenity);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<Booking> CreateBooking(
        Guid userId,
        Guid carId,
        BookingStatusEnum status,
        DateTime startTime,
        DateTime endTime
    )
    {
        var booking = new Booking
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            UserId = userId,
            CarId = carId,
            Status = status,
            StartTime = startTime,
            EndTime = endTime,
            ActualReturnTime = endTime,
            BasePrice = 100m,
            PlatformFee = 10m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 110m,
            Note = "Test booking",
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        return booking;
    }

    #endregion
}
