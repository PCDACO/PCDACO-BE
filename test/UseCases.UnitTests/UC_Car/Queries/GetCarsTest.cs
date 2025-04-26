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

        var handler = new GetCars.Handler(_dbContext, _geometryFactory);

        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
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
        Assert.All(result.Value.Items, item => Assert.Equal("Available", item.Status));
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

        var handler = new GetCars.Handler(_dbContext, _geometryFactory);

        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: manufacturer1.Id,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
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

        var handler = new GetCars.Handler(_dbContext, _geometryFactory);

        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: new[] { amenity1.Id, amenity2.Id },
            FuelTypes: null,
            TransmissionTypes: null,
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

        // Verify amenities in the response
        var responseAmenities = result.Value.Items.First().Amenities;
        Assert.Equal(2, responseAmenities.Length);
        Assert.Contains(responseAmenities, a => a.Id == amenity1.Id);
        Assert.Contains(responseAmenities, a => a.Id == amenity2.Id);
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

        var handler = new GetCars.Handler(_dbContext, _geometryFactory);

        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: electricFuel.Id,
            TransmissionTypes: automaticTransmission.Id,
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
        Assert.Equal("Electric", car.FuelType);
        Assert.Equal(automaticTransmission.Id, car.TransmissionId);
        Assert.Equal("Automatic", car.TransmissionType);
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

        var handler = new GetCars.Handler(_dbContext, _geometryFactory);

        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
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

        var handler = new GetCars.Handler(_dbContext, _geometryFactory);

        var query = new GetCars.Query(
            Latitude: (decimal)centerLatitude,
            Longtitude: (decimal)centerLongitude,
            Radius: 5000, // 5km radius
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            Keyword: string.Empty,
            StartTime: null,
            EndTime: null
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.TotalItems); // Only the first 2 cars should be within 5km

        // Verify that location data is included in the response
        foreach (var car in result.Value.Items)
        {
            Assert.NotNull(car.Location);
            // Verify cars are within reasonable distance
            var distance = Math.Sqrt(
                Math.Pow(car.Location.Latitude - centerLatitude, 2)
                    + Math.Pow(car.Location.Longtitude - centerLongitude, 2)
            );
            Assert.True(distance * 111320 < 5000); // Approximate conversion to meters
        }
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

        var handler = new GetCars.Handler(_dbContext, _geometryFactory);

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
            Keyword: string.Empty,
            StartTime: queryDate,
            EndTime: queryDate.AddDays(1)
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.TotalItems); // Car 1 and Car 3 should be available

        // Car 2 should be excluded due to booking conflict or marked as Rented
        var car2Status = result.Value.Items.FirstOrDefault(item => item.Id == car2.Id)?.Status;
        if (car2Status != null)
        {
            Assert.Equal("Rented", car2Status);
        }
        else
        {
            Assert.DoesNotContain(result.Value.Items, item => item.Id == car2.Id);
        }
    }

    [Fact]
    public async Task Handle_UsesPagination_Correctly()
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

        // Create 5 cars
        for (int i = 1; i <= 5; i++)
        {
            await CreateTestCar(
                owner.Id,
                model.Id,
                transmission.Id,
                fuelType.Id,
                CarStatusEnum.Available,
                $"CAR-{i:D5}"
            );
        }

        var handler = new GetCars.Handler(_dbContext, _geometryFactory);

        // First page query (2 items per page)
        var firstPageQuery = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            Keyword: string.Empty,
            StartTime: null,
            EndTime: null,
            PageNumber: 1,
            PageSize: 2
        );

        // Second page query
        var secondPageQuery = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            Keyword: string.Empty,
            StartTime: null,
            EndTime: null,
            PageNumber: 2,
            PageSize: 2
        );

        // Act
        var firstPageResult = await handler.Handle(firstPageQuery, CancellationToken.None);
        var secondPageResult = await handler.Handle(secondPageQuery, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, firstPageResult.Status);
        Assert.Equal(ResultStatus.Ok, secondPageResult.Status);

        Assert.Equal(5, firstPageResult.Value.TotalItems); // Total count should be 5
        Assert.Equal(2, firstPageResult.Value.Items.Count()); // First page has 2 items
        Assert.Equal(2, secondPageResult.Value.Items.Count()); // Second page has 2 items

        // Check that hasNext is correct
        Assert.True(firstPageResult.Value.HasNext);
        Assert.True(secondPageResult.Value.HasNext); // There should be a third page

        // Ensure no overlap between pages
        var firstPageIds = firstPageResult.Value.Items.Select(c => c.Id).ToList();
        var secondPageIds = secondPageResult.Value.Items.Select(c => c.Id).ToList();
        Assert.Empty(firstPageIds.Intersect(secondPageIds));
    }

    [Fact]
    public async Task Handle_ReturnsCarDetails_WithAllRelatedData()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(
            _dbContext,
            "BMW"
        );
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var amenities = await TestDataCreateAmenity.CreateTestAmenities(_dbContext);

        // Create car with specific details
        var car = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "BMW-X5-123",
            10.7756587,
            106.7004238,
            "Black",
            5,
            "Luxury SUV with all features",
            8.5m,
            250.00m,
            true
        );

        // Add amenities
        await AddAmenityToCar(car.Id, amenities[0].Id);
        await AddAmenityToCar(car.Id, amenities[1].Id);

        var handler = new GetCars.Handler(_dbContext, _geometryFactory);

        var query = new GetCars.Query(
            Latitude: null,
            Longtitude: null,
            Radius: null,
            ManufacturerId: null,
            Amenities: null,
            FuelTypes: null,
            TransmissionTypes: null,
            Keyword: string.Empty,
            StartTime: null,
            EndTime: null
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(1, result.Value.TotalItems);

        var carResponse = result.Value.Items.First();
        Assert.Equal("Test Model", carResponse.ModelName);
        Assert.Equal("Black", carResponse.Color);
        Assert.Equal(5, carResponse.Seat);
        Assert.Equal("Luxury SUV with all features", carResponse.Description);
        Assert.Equal("Automatic", carResponse.TransmissionType);
        Assert.Equal("Electric", carResponse.FuelType);
        Assert.Equal(8.5m, carResponse.FuelConsumption);
        Assert.True(carResponse.RequiresCollateral);
        Assert.Equal(250.00m, carResponse.Price);
        Assert.Equal("BMW", carResponse.Manufacturer.Name);
        Assert.Equal(2, carResponse.Amenities.Length);
        Assert.NotNull(carResponse.Location);
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
        double longitude = 106.7004238,
        string color = "Red",
        int seat = 4,
        string description = "Test car description",
        decimal fuelConsumption = 7.5m,
        decimal price = 100m,
        bool requiresCollateral = false
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
            Color = color,
            Seat = seat,
            Description = description,
            FuelConsumption = fuelConsumption,
            Price = price,
            RequiresCollateral = requiresCollateral,
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
