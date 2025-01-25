using System.Security.Cryptography;
using System.Text;
using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MockQueryable.Moq;
using Moq;
using NetTopologySuite.Geometries;
using Persistance.Data;
using Testcontainers.PostgreSql;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Car.Commands;
using UUIDNext;

namespace UseCases.UnitTests.UC_Car.Commands;

public class CreateCarTests : IAsyncLifetime
{
    private AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly GeometryFactory _geometryFactory;
    private readonly EncryptionSettings _encryptionSettings;
    private readonly IAesEncryptionService _aesService;
    private readonly IKeyManagementService _keyService;

    public CreateCarTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgis/postgis:latest")
            .WithCleanUp(true)
            .Build();

        _currentUser = new CurrentUser();
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        _encryptionSettings = new EncryptionSettings
        {
            Key = "dnjGHqR9O/2hKCQUgImXcEjZ9YPaAVcfz4l5VcTBLcY="
        };

        _aesService = new AesEncryptionService();
        _keyService = new KeyManagementService();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDBContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString(), o => o.UseNetTopologySuite())
            .EnableSensitiveDataLogging()
            .Options;

        _dbContext = new AppDBContext(options);
        await _dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task<User> CreateTestUser(UserRole role)
    {
        var (key, iv) = await _keyService.GenerateKeyAsync();
        var encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        var encryptionKey = new EncryptionKey
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptedKey = encryptedKey,
            IV = iv
        };

        _dbContext.EncryptionKeys.Add(encryptionKey);
        await _dbContext.SaveChangesAsync();

        var user = new User
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptionKeyId = encryptionKey.Id,
            Name = "Test User",
            Email = "test@example.com",
            Password = "password",
            Role = role,
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Phone = "1234567890"
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    private async Task<Manufacturer> CreateTestManufacturer()
    {
        var manufacturer = new Manufacturer
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = "Test Manufacturer"
        };

        _dbContext.Manufacturers.Add(manufacturer);
        await _dbContext.SaveChangesAsync();

        return manufacturer;
    }

    private async Task<List<Amenity>> CreateTestAmenities(int count = 2)
    {
        var amenities = Enumerable
            .Range(0, count)
            .Select(i => new Amenity
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = $"Amenity {i}",
                Description = $"Test Amenity {i}"
            })
            .ToList();

        _dbContext.Amenities.AddRange(amenities);
        await _dbContext.SaveChangesAsync();

        return amenities;
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
    public async Task Handle_UserIsAdmin_ReturnsError()
    {
        // Arrange
        var testUser = await CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        var handler = new CreateCar.Handler(
            _dbContext,
            _currentUser,
            _geometryFactory,
            _aesService,
            _keyService,
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
        var testUser = await CreateTestUser(UserRole.Driver);
        _currentUser.SetUser(testUser);
        var manufacturer = await CreateTestManufacturer();
        var amenities = await CreateTestAmenities();

        var handler = new CreateCar.Handler(
            _dbContext,
            _currentUser,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = CreateValidCommand(
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
        var user = await CreateTestUser(UserRole.Driver);
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

        var command = CreateValidCommand(manufacturerId: invalidManufacturerId);

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
        var user = await CreateTestUser(UserRole.Driver);
        _currentUser.SetUser(user);
        var manufacturer = await CreateTestManufacturer();
        var amenities = await CreateTestAmenities(3);

        var handler = new CreateCar.Handler(
            _dbContext,
            _currentUser,
            _geometryFactory,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = new CreateCar.Query(
            AmenityIds: [.. amenities.Select(a => a.Id)],
            ManufacturerId: manufacturer.Id,
            LicensePlate: "ABC-12345",
            Color: "Midnight Black",
            Seat: 5,
            Description: "Premium luxury vehicle",
            TransmissionType: TransmissionType.Auto,
            FuelType: FuelType.Hybrid,
            FuelConsumption: 6.2m,
            RequiresCollateral: true,
            PricePerHour: 75m,
            PricePerDay: 650m,
            Latitude: 40.7128m,
            Longtitude: -74.0060m
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
