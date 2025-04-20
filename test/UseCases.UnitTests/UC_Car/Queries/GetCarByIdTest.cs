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
public class GetCarByIdTest(DatabaseTestBase fixture) : IAsyncLifetime
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
    public async Task Handle_CarNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new GetCarById.Handler(_dbContext, _currentUser);

        var query = new GetCarById.Query(Guid.NewGuid());

        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(user);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.CarNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_AsCarOwner_ReturnsCarWithContractDetails()
    {
        // Arrange
        // Create owner
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        // Create car
        var car = await CreateTestCar(owner.Id, "OWNER-123");

        // Create contract for the car
        await CreateCarContract(car.Id, CarContractStatusEnum.OwnerSigned);

        var handler = new GetCarById.Handler(_dbContext, _currentUser);

        var query = new GetCarById.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(car.Id, result.Value.Id);
        Assert.NotNull(result.Value.Contract); // Owner should be able to see contract details
        Assert.Equal("OWNER-123", result.Value.LicensePlate); // License plate should be decrypted
    }

    [Fact]
    public async Task Handle_AsAdmin_ReturnsCarWithContractDetails()
    {
        // Arrange
        // Create owner and admin
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);

        _currentUser.SetUser(admin);

        // Create car
        var car = await CreateTestCar(owner.Id, "ADMIN-456");

        // Create contract for the car
        await CreateCarContract(car.Id, CarContractStatusEnum.OwnerSigned);

        var handler = new GetCarById.Handler(_dbContext, _currentUser);

        var query = new GetCarById.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(car.Id, result.Value.Id);
        Assert.NotNull(result.Value.Contract); // Admin should be able to see contract details
    }

    [Fact]
    public async Task Handle_AsConsultant_ReturnsCarWithContractDetails()
    {
        // Arrange
        // Create owner and consultant
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        _currentUser.SetUser(consultant);

        // Create car
        var car = await CreateTestCar(owner.Id, "CONSLT-789");

        // Create contract for the car
        await CreateCarContract(car.Id, CarContractStatusEnum.OwnerSigned);

        var handler = new GetCarById.Handler(_dbContext, _currentUser);

        var query = new GetCarById.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(car.Id, result.Value.Id);
        Assert.NotNull(result.Value.Contract); // Consultant should be able to see contract details
    }

    [Fact]
    public async Task Handle_AsTechnician_ReturnsCarWithContractDetails()
    {
        // Arrange
        // Create owner and technician
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);

        _currentUser.SetUser(technician);

        // Create car
        var car = await CreateTestCar(owner.Id, "TECH-444");

        // Create contract for the car
        await CreateCarContract(car.Id, CarContractStatusEnum.OwnerSigned);

        var handler = new GetCarById.Handler(_dbContext, _currentUser);

        var query = new GetCarById.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(car.Id, result.Value.Id);
        Assert.NotNull(result.Value.Contract); // Technician should be able to see contract details
    }

    [Fact]
    public async Task Handle_AsOtherUser_ReturnsCarWithoutContractDetails()
    {
        // Arrange
        // Create owner and driver
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);

        _currentUser.SetUser(driver);

        // Create car
        var car = await CreateTestCar(owner.Id, "DRIVER-555");

        // Create contract for the car
        await CreateCarContract(car.Id, CarContractStatusEnum.OwnerSigned);

        var handler = new GetCarById.Handler(_dbContext, _currentUser);

        var query = new GetCarById.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(car.Id, result.Value.Id);
        Assert.Null(result.Value.Contract); // Driver should not see contract details
    }

    [Fact]
    public async Task Handle_DecryptsLicensePlate_Correctly()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        // Create car with known license plate
        string expectedLicensePlate = "ABC-123XY";
        var car = await CreateTestCar(owner.Id, expectedLicensePlate);

        var handler = new GetCarById.Handler(_dbContext, _currentUser);

        var query = new GetCarById.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(expectedLicensePlate, result.Value.LicensePlate);
    }

    [Fact]
    public async Task Handle_IncludesAllRelatedEntities()
    {
        // Arrange
        // Create roles
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        // Create owner and driver
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(owner);

        // Create car
        var car = await CreateTestCar(owner.Id, "TEST-789");

        // Add amenities
        var amenities = await TestDataCreateAmenity.CreateTestAmenities(_dbContext);
        await AddAmenityToCar(car.Id, amenities[0].Id);
        await AddAmenityToCar(car.Id, amenities[1].Id);

        // Add bookings
        var now = DateTimeOffset.UtcNow;
        await CreateBooking(
            driver.Id,
            car.Id,
            BookingStatusEnum.Approved,
            now.AddDays(1),
            now.AddDays(3)
        );
        await CreateBooking(
            driver.Id,
            car.Id,
            BookingStatusEnum.Approved,
            now.AddDays(5),
            now.AddDays(7)
        );
        await CreateBooking(
            driver.Id,
            car.Id,
            BookingStatusEnum.Cancelled,
            now.AddDays(10),
            now.AddDays(12)
        );

        var handler = new GetCarById.Handler(_dbContext, _currentUser);

        var query = new GetCarById.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Check car details
        Assert.Equal(car.Id, result.Value.Id);
        Assert.Equal(owner.Id, result.Value.OwnerId);
        Assert.Equal(owner.Name, result.Value.OwnerName);

        // Check amenities
        Assert.Equal(2, result.Value.Amenities.Length);

        // Check bookings (only future non-cancelled should be included)
        Assert.Equal(3, result.Value.Bookings.Length);
        Assert.All(result.Value.Bookings, b => Assert.Equal(driver.Id, b.DriverId));
        Assert.All(result.Value.Bookings, b => Assert.Equal(driver.Name, b.DriverName));

        // Check manufacturer details
        Assert.NotNull(result.Value.Manufacturer);

        // Check location
        Assert.NotNull(result.Value.PickupLocation);
    }

    #region Helper Methods

    private async Task<Car> CreateTestCar(Guid ownerId, string licensePlate)
    {
        // Create prerequisites
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmission = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create pickup location point
        var pickupLocation = _geometryFactory.CreatePoint(new Coordinate(106.7004238, 10.7756587));
        pickupLocation.SRID = 4326;

        // Create car
        Guid carId = Uuid.NewDatabaseFriendly(Database.PostgreSql);
        var car = new Car
        {
            Id = carId,
            OwnerId = ownerId,
            ModelId = model.Id,
            LicensePlate = licensePlate,
            FuelTypeId = fuelType.Id,
            TransmissionTypeId = transmission.Id,
            Status = CarStatusEnum.Available,
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
        await CreateGPSForCar(car.Id);

        // Create image type and images
        var imageType = await CreateOrGetCarImageType();
        await _dbContext.ImageCars.AddAsync(
            new ImageCar
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                TypeId = imageType.Id,
                Url = "http://example.com/image1.jpg",
                Name = "Main Image",
                IsDeleted = false,
            }
        );

        await _dbContext.SaveChangesAsync();
        return car;
    }

    private async Task<CarGPS> CreateGPSForCar(Guid carId)
    {
        var gpsDevice = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext);
        var location = _geometryFactory.CreatePoint(new Coordinate(106.7004238, 10.7756587));
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

    private async Task<Booking> CreateBooking(
        Guid userId,
        Guid carId,
        BookingStatusEnum status,
        DateTimeOffset startTime,
        DateTimeOffset endTime
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
