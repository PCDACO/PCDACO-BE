using System.Security.Cryptography;
using System.Text;
using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MockQueryable.Moq;
using Moq;
using NetTopologySuite.Geometries;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Car.Commands;
using UUIDNext;

namespace UseCases.UnitTests.UC_Car.Commands;

public class CreateCarTests
{
    private readonly Mock<IAppDBContext> _mockContext;
    private readonly CurrentUser _currentUser;
    private readonly Mock<IAesEncryptionService> _mockAesService;
    private readonly Mock<IKeyManagementService> _mockKeyService;
    private readonly GeometryFactory _geometryFactory;
    private readonly EncryptionSettings _encryptionSettings;

    public CreateCarTests()
    {
        _mockContext = new Mock<IAppDBContext>();
        _currentUser = new CurrentUser();
        _mockAesService = new Mock<IAesEncryptionService>();
        _mockKeyService = new Mock<IKeyManagementService>();
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        _encryptionSettings = new EncryptionSettings
        {
            Key = "dnjGHqR9O/2hKCQUgImXcEjZ9YPaAVcfz4l5VcTBLcY="
        };
    }

    private static User CreateTestUser(UserRole role)
    {
        return new User
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptionKeyId = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = "Test User",
            Email = "test@example.com",
            Password = "password",
            Role = role,
            Address = "Test Address",
            DateOfBirth = DateTime.Now.AddYears(-30),
            Phone = "1234567890"
        };
    }

    private static Amenity CreateTestAmenity()
    {
        return new Amenity
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = "WiFi",
            Description = "High-speed internet"
        };
    }

    private static Manufacturer CreateTestManufacturer() =>
        new() { Id = Uuid.NewDatabaseFriendly(Database.PostgreSql), Name = "Toyota" };

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
        where T : class
    {
        var mockSet = data.AsQueryable().BuildMockDbSet();
        return mockSet;
    }

    [Fact]
    public async Task Handle_UserIsAdmin_ReturnsError()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        var handler = new CreateCar.Handler(
            _mockContext.Object,
            _currentUser,
            _geometryFactory,
            _mockAesService.Object,
            _mockKeyService.Object,
            _encryptionSettings
        );

        var command = CreateValidCommand();

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
        var testUser = CreateTestUser(UserRole.Driver);
        _currentUser.SetUser(testUser);

        // Mock amenities with only 1 item (request has 2 IDs)
        var amenities = new List<Amenity> { CreateTestAmenity() };
        var mockAmenities = CreateMockDbSet(amenities);
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        // Mock valid manufacturer
        var manufacturer = CreateTestManufacturer();
        var mockManufacturers = CreateMockDbSet([manufacturer]);
        _mockContext.Setup(c => c.Manufacturers).Returns(mockManufacturers.Object);

        var handler = new CreateCar.Handler(
            _mockContext.Object,
            _currentUser,
            _geometryFactory,
            _mockAesService.Object,
            _mockKeyService.Object,
            _encryptionSettings
        );

        var command = CreateValidCommand(
            manufacturerId: manufacturer.Id,
            amenityIds: [amenities[0].Id, Guid.NewGuid()] // One invalid ID
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Một số tiện nghi không tồn tại !", result.Errors);
    }

    [Fact]
    public async Task Handle_MissingManufacturer_ReturnsError()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Driver);
        _currentUser.SetUser(testUser);

        // Mock empty manufacturers
        var mockManufacturers = CreateMockDbSet(new List<Manufacturer>());
        _mockContext.Setup(c => c.Manufacturers).Returns(mockManufacturers.Object);

        var handler = new CreateCar.Handler(
            _mockContext.Object,
            _currentUser,
            _geometryFactory,
            _mockAesService.Object,
            _mockKeyService.Object,
            _encryptionSettings
        );

        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Hãng xe không tồn tại !", result.Errors);
    }

    private CreateCar.Query CreateValidCommand(
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
            TransmissionType: TransmissionType.Auto,
            FuelType: FuelType.Petrol,
            FuelConsumption: 7.5m,
            RequiresCollateral: true,
            PricePerHour: 50m,
            PricePerDay: 500m,
            Latitude: 10.5m,
            Longtitude: 106.5m
        );

    [Fact]
    public void Validator_InvalidLicensePlate_ReturnsErrors()
    {
        // Arrange
        var validator = new CreateCar.Validator();
        var invalidCommand = new CreateCar.Query(
            AmenityIds: [],
            ManufacturerId: Guid.Empty,
            LicensePlate: "SHORT",
            Color: "",
            Seat: 0,
            Description: new string('a', 501),
            TransmissionType: TransmissionType.Auto,
            FuelType: FuelType.Electric,
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

        // Create test user and set current user
        var testUser = CreateTestUser(UserRole.Driver);
        _currentUser.SetUser(testUser);

        // Setup mock amenities
        var amenities = new List<Amenity> { CreateTestAmenity(), CreateTestAmenity() };
        var mockAmenities = CreateMockDbSet(amenities);
        _mockContext.Setup(c => c.Amenities).Returns(mockAmenities.Object);

        // Setup mock manufacturer
        var manufacturer = CreateTestManufacturer();
        var mockManufacturers = CreateMockDbSet([manufacturer]);
        _mockContext.Setup(c => c.Manufacturers).Returns(mockManufacturers.Object);

        // Setup encryption key tracking
        var encryptionKeysList = new List<EncryptionKey>();
        var mockEncryptionKeys = new Mock<DbSet<EncryptionKey>>();
        mockEncryptionKeys
            .Setup(m => m.Add(It.IsAny<EncryptionKey>()))
            .Callback<EncryptionKey>(encryptionKeysList.Add)
            .Returns((EncryptionKey key) => null!);

        _mockContext.Setup(c => c.EncryptionKeys).Returns(mockEncryptionKeys.Object);

        // Setup car tracking
        var carsList = new List<Car>();
        var mockCars = new Mock<DbSet<Car>>();
        mockCars
            .Setup(m => m.AddAsync(It.IsAny<Car>(), It.IsAny<CancellationToken>()))
            .Callback<Car, CancellationToken>((car, _) => carsList.Add(car))
            .ReturnsAsync((Car car, CancellationToken _) => null!);

        _mockContext.Setup(c => c.Cars).Returns(mockCars.Object);

        // Mock cryptographic services
        _mockKeyService.Setup(k => k.GenerateKeyAsync()).ReturnsAsync(("testKey", "testIV"));
        _mockKeyService
            .Setup(k => k.EncryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("encryptedTestKey");

        _mockAesService
            .Setup(a => a.Encrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("encryptedLicense");

        // Mock database save operation
        _mockContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Create handler with mocked dependencies
        var handler = new CreateCar.Handler(
            _mockContext.Object,
            _currentUser,
            _geometryFactory,
            _mockAesService.Object,
            _mockKeyService.Object,
            _encryptionSettings
        );

        // Create command with valid data
        var command = CreateValidCommand(
            manufacturerId: manufacturer.Id,
            amenityIds: [.. amenities.Select(a => a.Id)]
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        // Verify overall success
        Assert.Equal(ResultStatus.Created, result.Status);

        // Verify encryption key
        Assert.Single(encryptionKeysList);
        var encryptionKey = encryptionKeysList[0];
        Assert.Equal("encryptedTestKey", encryptionKey.EncryptedKey);
        Assert.Equal("testIV", encryptionKey.IV);

        // Verify car creation
        Assert.Single(carsList);
        var createdCar = carsList[0];

        Assert.Equal(testUser.Id, createdCar.OwnerId);
        Assert.Equal(manufacturer.Id, createdCar.ManufacturerId);
        Assert.Equal("encryptedLicense", createdCar.EncryptedLicensePlate);
        Assert.Equal(encryptionKey.Id, createdCar.EncryptionKeyId);
        Assert.Equal(command.Color, createdCar.Color);
        Assert.Equal(command.Seat, createdCar.Seat);
        Assert.Equal(command.Description, createdCar.Description);
        Assert.Equal(command.TransmissionType, createdCar.TransmissionType);
        Assert.Equal(command.FuelType, createdCar.FuelType);
        Assert.Equal(command.FuelConsumption, createdCar.FuelConsumption);
        Assert.Equal(command.RequiresCollateral, createdCar.RequiresCollateral);
        Assert.Equal(command.PricePerHour, createdCar.PricePerHour);
        Assert.Equal(command.PricePerDay, createdCar.PricePerDay);

        // Verify location
        var expectedPoint = _geometryFactory.CreatePoint(
            new Coordinate((double)command.Longtitude!, (double)command.Latitude!)
        );
        Assert.Equal(expectedPoint, createdCar.Location);

        // Verify amenities mapping
        Assert.Equal(command.AmenityIds.Length, createdCar.CarAmenities.Count);
        Assert.All(
            createdCar.CarAmenities,
            ca =>
            {
                Assert.Equal(createdCar.Id, ca.CarId);
                Assert.Contains(ca.AmenityId, command.AmenityIds);
            }
        );

        // Verify statistics
        Assert.NotNull(createdCar.CarStatistic);
        Assert.Equal(createdCar.Id, createdCar.CarStatistic.CarId);

        // Verify database commit
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
