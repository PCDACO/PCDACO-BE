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
using UseCases.Utils;
using UUIDNext;

namespace UseCases.UnitTests.UC_Car.Queries;

[Collection("Test Collection")]
public class GetCarsForStaffsTest(DatabaseTestBase fixture) : IAsyncLifetime
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
    public async Task Handle_ReturnsAllCars_WithDifferentStatuses()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await CreateUserWithEncryptedPhone(adminRole.Id);
        _currentUser.SetUser(admin);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedPhone(ownerRole.Id, "owner@test.com");

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
            "ABC-12345"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Pending,
            "XYZ-67890"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Rented,
            "DEF-24680"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Rejected,
            "GHI-13579"
        );

        var handler = new GetCarsForStaffs.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetCarsForStaffs.Query(
            PageNumber: 1,
            PageSize: 10,
            Keyword: "",
            Status: null
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(4, result.Value.TotalItems); // Should return all 4 cars
    }

    [Fact]
    public async Task Handle_FiltersByCarsStatus_WhenStatusProvided()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await CreateUserWithEncryptedPhone(adminRole.Id);
        _currentUser.SetUser(admin);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedPhone(ownerRole.Id, "owner@test.com");

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
            "ABC-12345"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Pending,
            "XYZ-67890"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Pending,
            "DEF-24680"
        );
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Rejected,
            "GHI-13579"
        );

        var handler = new GetCarsForStaffs.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetCarsForStaffs.Query(
            PageNumber: 1,
            PageSize: 10,
            Keyword: "",
            Status: CarStatusEnum.Pending
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.TotalItems); // Should return 2 pending cars only
        Assert.All(result.Value.Items, item => Assert.Equal("Pending", item.Status));
    }

    [Fact]
    public async Task Handle_FiltersByKeyword_WhenKeywordProvided()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await CreateUserWithEncryptedPhone(adminRole.Id);
        _currentUser.SetUser(admin);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedPhone(ownerRole.Id, "owner@test.com");

        // Create manufacturers and models with specific names
        var manufacturer1 = await TestDataCreateManufacturer.CreateTestManufacturer(
            _dbContext,
            "Toyota"
        );
        var manufacturer2 = await TestDataCreateManufacturer.CreateTestManufacturer(
            _dbContext,
            "Honda"
        );

        var model1 = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer1.Id);
        model1.Name = "Corolla Sedan";
        await _dbContext.SaveChangesAsync();

        var model2 = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer2.Id);
        model2.Name = "Civic Hatchback";
        await _dbContext.SaveChangesAsync();

        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create cars with different models
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
            model2.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "XYZ-67890"
        );

        var handler = new GetCarsForStaffs.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetCarsForStaffs.Query(
            PageNumber: 1,
            PageSize: 10,
            Keyword: "Sedan",
            Status: null
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal("Corolla Sedan", result.Value.Items.First().ModelName);
    }

    [Fact]
    public async Task Handle_Pagination_WorksCorrectly()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await CreateUserWithEncryptedPhone(adminRole.Id);
        _currentUser.SetUser(admin);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedPhone(ownerRole.Id, "owner@test.com");

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

        var handler = new GetCarsForStaffs.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        // First page - 2 items
        var firstPageQuery = new GetCarsForStaffs.Query(
            PageNumber: 1,
            PageSize: 2,
            Keyword: "",
            Status: null
        );

        // Second page - 2 items
        var secondPageQuery = new GetCarsForStaffs.Query(
            PageNumber: 2,
            PageSize: 2,
            Keyword: "",
            Status: null
        );

        // Third page - 1 item
        var thirdPageQuery = new GetCarsForStaffs.Query(
            PageNumber: 3,
            PageSize: 2,
            Keyword: "",
            Status: null
        );

        // Act
        var firstPageResult = await handler.Handle(firstPageQuery, CancellationToken.None);
        var secondPageResult = await handler.Handle(secondPageQuery, CancellationToken.None);
        var thirdPageResult = await handler.Handle(thirdPageQuery, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, firstPageResult.Status);
        Assert.Equal(ResultStatus.Ok, secondPageResult.Status);
        Assert.Equal(ResultStatus.Ok, thirdPageResult.Status);

        Assert.Equal(5, firstPageResult.Value.TotalItems); // Total items should be consistent
        Assert.Equal(5, secondPageResult.Value.TotalItems);
        Assert.Equal(5, thirdPageResult.Value.TotalItems);

        Assert.Equal(2, firstPageResult.Value.Items.Count()); // First page has 2 items
        Assert.Equal(2, secondPageResult.Value.Items.Count()); // Second page has 2 items
        Assert.Single(thirdPageResult.Value.Items); // Third page has 1 item

        // Check that the pages have different items
        var allCarIds = firstPageResult
            .Value.Items.Concat(secondPageResult.Value.Items)
            .Concat(thirdPageResult.Value.Items)
            .Select(c => c.Id)
            .ToList();

        Assert.Equal(5, allCarIds.Distinct().Count()); // All items should be unique
    }

    [Fact]
    public async Task Handle_DecryptsLicensePlate_Correctly()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await CreateUserWithEncryptedPhone(adminRole.Id);
        _currentUser.SetUser(admin);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedPhone(ownerRole.Id, "owner@test.com");

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

        var handler = new GetCarsForStaffs.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetCarsForStaffs.Query(
            PageNumber: 1,
            PageSize: 10,
            Keyword: "",
            Status: null
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal(plainLicensePlate, result.Value.Items.First().LicensePlate);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectCarDetails_WithAllRelatedData()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await CreateUserWithEncryptedPhone(adminRole.Id);
        _currentUser.SetUser(admin);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await CreateUserWithEncryptedPhone(ownerRole.Id, "owner@test.com");

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(
            _dbContext,
            "Luxury Cars"
        );
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        model.Name = "Premium Model";
        await _dbContext.SaveChangesAsync();

        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create a car with specific details
        var car = await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Available,
            "LUXURY-99",
            color: "Midnight Blue",
            seat: 5,
            description: "Premium luxury vehicle",
            fuelConsumption: 5.8m,
            price: 299.99m,
            requiresCollateral: true
        );

        // Add amenities to car
        var amenities = await TestDataCreateAmenity.CreateTestAmenities(_dbContext);
        await AddAmenityToCar(car.Id, amenities[0].Id);
        await AddAmenityToCar(car.Id, amenities[1].Id);

        var handler = new GetCarsForStaffs.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetCarsForStaffs.Query(
            PageNumber: 1,
            PageSize: 10,
            Keyword: "",
            Status: null
        );

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(1, result.Value.TotalItems);

        var carResponse = result.Value.Items.First();
        Assert.Equal("Premium Model", carResponse.ModelName);
        Assert.Equal("Test User", carResponse.OwnerName);
        Assert.Equal("LUXURY-99", carResponse.LicensePlate);
        Assert.Equal("Midnight Blue", carResponse.Color);
        Assert.Equal(5, carResponse.Seat);
        Assert.Equal("Premium luxury vehicle", carResponse.Description);
        Assert.Equal("Automatic", carResponse.TransmissionType);
        Assert.Equal("Electric", carResponse.FuelType);
        Assert.Equal(5.8m, carResponse.FuelConsumption);
        Assert.True(carResponse.RequiresCollateral);
        Assert.Equal(299.99m, carResponse.Price);
        Assert.Equal(2, carResponse.Amenities.Length);
        Assert.Equal("Luxury Cars", carResponse.Manufacturer.Name);
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
        await CreateGPSForCar(car.Id, latitude, longitude);

        // Create image for the car
        await CreateCarImage(car.Id);

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

    private async Task<User> CreateUserWithEncryptedPhone(
        Guid roleId,
        string email = "test@example.com",
        string name = "Test User",
        string phoneNumber = "0123456789"
    )
    {
        // Generate encryption key and encrypt phone number
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedPhoneNumber = await _aesService.Encrypt(phoneNumber, key, iv);
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

        // Create user with encrypted phone number
        var user = new User
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = name,
            Password = "password".HashString(),
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Email = email,
            RoleId = roleId,
            Phone = encryptedPhoneNumber,
            EncryptionKeyId = encryptionKey.Id,
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    #endregion
}
