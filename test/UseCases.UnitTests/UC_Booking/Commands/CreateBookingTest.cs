using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Persistance.Data;
using Testcontainers.PostgreSql;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Booking.Commands;
using UUIDNext;

namespace UseCases.UnitTests.UC_Booking.Commands;

public class CreateBookingTests : IAsyncLifetime
{
    private AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly EncryptionSettings _encryptionSettings;
    private readonly IKeyManagementService _keyService;

    public CreateBookingTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgis/postgis:latest")
            .WithCleanUp(true)
            .Build();

        _currentUser = new CurrentUser();
        _encryptionSettings = new EncryptionSettings
        {
            Key = "dnjGHqR9O/2hKCQUgImXcEjZ9YPaAVcfz4l5VcTBLcY="
        };
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
        var encryptionKey = await CreateTestEncryptKey();

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

    private async Task<Car> CreateTestCar(User user)
    {
        var encryptionKey = await CreateTestEncryptKey();
        var manufacturer = await CreateTestManufacturer();

        var car = new Car
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OwnerId = user.Id,
            ManufacturerId = manufacturer.Id,
            EncryptionKeyId = encryptionKey.Id,
            EncryptedLicensePlate = "ABC123",
            Color = "Red",
            Seat = 4,
            FuelConsumption = 5.5m,
            PricePerDay = 100m,
            PricePerHour = 10m,
            Location = new Point(0, 0)
        };

        _dbContext.Cars.Add(car);
        await _dbContext.SaveChangesAsync();
        return car;
    }

    private async Task<EncryptionKey> CreateTestEncryptKey()
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

        return encryptionKey;
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

    [Fact]
    public async Task Handle_UserNotDriver_ReturnsError()
    {
        // Arrange
        var testUser = await CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        var handler = new CreateBooking.Handler(_dbContext, _currentUser);
        var command = new CreateBooking.CreateBookingCommand(
            testUser.Id,
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này !", result.Errors);
    }

    [Fact]
    public async Task Handle_CarNotFound_ReturnsNotFound()
    {
        // Arrange
        var testUser = await CreateTestUser(UserRole.Driver);
        _currentUser.SetUser(testUser);

        var handler = new CreateBooking.Handler(_dbContext, _currentUser);
        var command = new CreateBooking.CreateBookingCommand(
            testUser.Id,
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesBookingWithCorrectValues()
    {
        // Arrange
        var testUser = await CreateTestUser(UserRole.Driver);
        var testCar = await CreateTestCar(testUser);
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow.AddDays(3);

        _currentUser.SetUser(testUser);

        var handler = new CreateBooking.Handler(_dbContext, _currentUser);
        var command = new CreateBooking.CreateBookingCommand(
            testUser.Id,
            testCar.Id,
            startTime,
            endTime
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var createdBooking = await _dbContext.Bookings.FirstOrDefaultAsync(b =>
            b.Id == result.Value.Id
        );

        Assert.NotNull(createdBooking);
        Assert.Equal(testUser.Id, createdBooking.UserId);
        Assert.Equal(testCar.Id, createdBooking.CarId);
    }

    [Fact]
    public void Validator_EndTimeBeforeStartTime_ReturnsValidationError()
    {
        // Arrange
        var validator = new CreateBooking.Validator();
        var command = new CreateBooking.CreateBookingCommand(
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1)
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage == "Thời gian kết thúc thuê phải sau thời gian bắt đầu thuê"
        );
    }

    [Fact]
    public async Task Handle_SameUserOverlappingBooking_ReturnsConflict()
    {
        // Arrange
        var testUser = await CreateTestUser(UserRole.Driver);
        var testCar = await CreateTestCar(testUser);
        _currentUser.SetUser(testUser);

        // Create existing booking
        var existingBooking = new Booking
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            UserId = testUser.Id,
            CarId = testCar.Id,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(3),
            ActualReturnTime = DateTime.UtcNow.AddHours(3),
            BasePrice = 100m,
            PlatformFee = 10m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 110m,
            Note = "Test note"
        };

        _dbContext.Bookings.Add(existingBooking);
        await _dbContext.SaveChangesAsync();

        // Create overlapping command
        var command = new CreateBooking.CreateBookingCommand(
            testUser.Id,
            testCar.Id,
            DateTime.UtcNow.AddHours(2), // Overlaps
            DateTime.UtcNow.AddHours(4)
        );

        var handler = new CreateBooking.Handler(_dbContext, _currentUser);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Handle_DifferentUserOverlappingBooking_CreatesSuccessfully()
    {
        // Arrange
        var testUser1 = await CreateTestUser(UserRole.Driver);
        var testUser2 = await CreateTestUser(UserRole.Driver);
        var testCar = await CreateTestCar(testUser1);

        // Create existing booking for User1
        var existingBooking = new Booking
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            UserId = testUser1.Id,
            CarId = testCar.Id,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(3),
            ActualReturnTime = DateTime.UtcNow.AddHours(3),
            BasePrice = 100m,
            PlatformFee = 10m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 110m,
            Note = "Test note"
        };

        _dbContext.Bookings.Add(existingBooking);
        await _dbContext.SaveChangesAsync();

        // Use User2 for new booking
        _currentUser.SetUser(testUser2);
        var handler = new CreateBooking.Handler(_dbContext, _currentUser);
        var command = new CreateBooking.CreateBookingCommand(
            testUser2.Id,
            testCar.Id,
            DateTime.UtcNow.AddHours(2), // Overlaps
            DateTime.UtcNow.AddHours(4)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        var newBooking = await _dbContext.Bookings.FirstOrDefaultAsync(b =>
            b.UserId == testUser2.Id
        );
        Assert.NotNull(newBooking);
    }
}
