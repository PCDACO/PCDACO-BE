using Ardalis.Result;
using Domain.Constants;
using Domain.Constants.EntityNames;
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
public class GetRentedCarsTest(DatabaseTestBase fixture) : IAsyncLifetime
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
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner); // Set current user as non-admin

        var handler = new GetRentedCars.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetRentedCars.Query(PageNumber: 1, PageSize: 10, Keyword: "");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyRentedCars()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(admin);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com"
        );

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
            CarStatusEnum.Rented,
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
            CarStatusEnum.Rented,
            "GHI-13579"
        );

        var handler = new GetRentedCars.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetRentedCars.Query(PageNumber: 1, PageSize: 10, Keyword: "");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.TotalItems); // Only 2 rented cars
        Assert.All(
            result.Value.Items,
            car =>
                Assert.True(
                    car.LicensePlate == "XYZ-67890" || car.LicensePlate == "GHI-13579",
                    $"License plate {car.LicensePlate} should be either 'XYZ-67890' or 'GHI-13579'"
                )
        );
    }

    [Fact]
    public async Task Handle_OrdersByFeedbackPoints()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(admin);

        // Create two owners with different feedback ratings
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner1 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner1@test.com"
        );
        var owner2 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner2@test.com"
        );

        // Create driver role for feedback
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
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

        // Create rented cars for both owners
        var car1 = await CreateTestCar(
            owner1.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Rented,
            "CAR1-123"
        );
        var car2 = await CreateTestCar(
            owner2.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Rented,
            "CAR2-456"
        );

        // Create bookings and feedbacks (owner1 gets lower rating than owner2)
        var booking1 = await CreateTestBooking(driver.Id, car1.Id);
        var booking2 = await CreateTestBooking(driver.Id, car2.Id);

        await CreateFeedback(booking1.Id, driver.Id, owner1.Id, FeedbackTypeEnum.ToOwner, 3); // Lower rating
        await CreateFeedback(booking2.Id, driver.Id, owner2.Id, FeedbackTypeEnum.ToOwner, 5); // Higher rating

        var handler = new GetRentedCars.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetRentedCars.Query(PageNumber: 1, PageSize: 10, Keyword: "");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.TotalItems);

        // First car should be from owner2 (higher rating)
        Assert.Equal("CAR2-456", result.Value.Items.First().LicensePlate);
        // Second car should be from owner1 (lower rating)
        Assert.Equal("CAR1-123", result.Value.Items.Last().LicensePlate);
    }

    [Fact]
    public async Task Handle_Pagination_WorksCorrectly()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(admin);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com"
        );

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create 5 rented cars
        for (int i = 1; i <= 5; i++)
        {
            await CreateTestCar(
                owner.Id,
                model.Id,
                transmission.Id,
                fuelType.Id,
                CarStatusEnum.Rented,
                $"RENT-{i:D2}"
            );
        }

        var handler = new GetRentedCars.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        // First page - 2 items
        var firstPageQuery = new GetRentedCars.Query(PageNumber: 1, PageSize: 2, Keyword: "");

        // Second page - 2 items
        var secondPageQuery = new GetRentedCars.Query(PageNumber: 2, PageSize: 2, Keyword: "");

        // Third page - 1 item
        var thirdPageQuery = new GetRentedCars.Query(PageNumber: 3, PageSize: 2, Keyword: "");

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
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(admin);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create a car with known license plate
        string plainLicensePlate = "SPECIAL-9876";
        await CreateTestCar(
            owner.Id,
            model.Id,
            transmission.Id,
            fuelType.Id,
            CarStatusEnum.Rented,
            plainLicensePlate
        );

        var handler = new GetRentedCars.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetRentedCars.Query(PageNumber: 1, PageSize: 10, Keyword: "");

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
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(admin);

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com"
        );

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(
            _dbContext,
            "Premium Auto"
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
            CarStatusEnum.Rented,
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

        var handler = new GetRentedCars.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetRentedCars.Query(PageNumber: 1, PageSize: 10, Keyword: "");

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
        Assert.Equal("Premium Auto", carResponse.Manufacturer.Name);
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

        await _dbContext.SaveChangesAsync();
        return car;
    }

    private async Task<Booking> CreateTestBooking(
        Guid userId,
        Guid carId,
        BookingStatusEnum status = BookingStatusEnum.Completed,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null
    )
    {
        var booking = new Booking
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            UserId = userId,
            CarId = carId,
            Status = status,
            StartTime = startTime ?? DateTimeOffset.UtcNow.AddDays(-7),
            EndTime = endTime ?? DateTimeOffset.UtcNow.AddDays(-5),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(-5),
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

    private async Task<Feedback> CreateFeedback(
        Guid bookingId,
        Guid userId,
        Guid targetUserId,
        FeedbackTypeEnum type,
        int rating
    )
    {
        var feedback = new Feedback
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            BookingId = bookingId,
            UserId = userId,
            Type = type,
            Point = rating,
            Content = $"Test feedback with rating {rating}",
        };

        await _dbContext.Feedbacks.AddAsync(feedback);
        await _dbContext.SaveChangesAsync();

        return feedback;
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

    #endregion
}
