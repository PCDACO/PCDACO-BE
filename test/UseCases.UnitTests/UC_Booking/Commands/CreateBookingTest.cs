using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using NetTopologySuite.Geometries;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Booking.Commands;
using UUIDNext;

namespace UseCases.UnitTests.UC_Booking.Commands;

public class CreateBookingTests
{
    private readonly Mock<IAppDBContext> _mockContext;
    private readonly CurrentUser _currentUser;

    public CreateBookingTests()
    {
        _mockContext = new Mock<IAppDBContext>();
        _currentUser = new CurrentUser();
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

    private static Car CreateTestCar()
    {
        return new Car
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OwnerId = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            ManufacturerId = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptionKeyId = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptedLicensePlate = "ABC123",
            Color = "Red",
            Seat = 4,
            FuelConsumption = 5.5m,
            PricePerDay = 100m,
            PricePerHour = 10m,
            Location = new Point(0, 0)
        };
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
        where T : class
    {
        // Use MockQueryable.Moq to create a mock DbSet from in-memory data
        var mockSet = data.BuildMock().BuildMockDbSet();

        // Setup FindAsync to return the first item in the list (simulates EF's FindAsync)
        mockSet
            .Setup(x => x.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync((object[] ids) => data.FirstOrDefault());

        return mockSet;
    }

    [Fact]
    public async Task Handle_UserNotDriver_ReturnsError()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Admin);
        _currentUser.SetUser(testUser);

        var handler = new CreateBooking.Handler(_mockContext.Object, _currentUser);
        var command = new CreateBooking.CreateBookingCommand(
            testUser.Id,
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            DateTime.Now,
            DateTime.Now.AddDays(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Bạn không có quyền thực hiện chức năng này !", result.Errors.First());
    }

    [Fact]
    public async Task Handle_CarNotFound_ReturnsNotFound()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Driver);
        _currentUser.SetUser(testUser);

        // Mock empty cars DbSet
        var mockCars = CreateMockDbSet(new List<Car>());
        _mockContext.Setup(c => c.Cars).Returns(mockCars.Object);

        var handler = new CreateBooking.Handler(_mockContext.Object, _currentUser);
        var command = new CreateBooking.CreateBookingCommand(
            testUser.Id,
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            DateTime.Now,
            DateTime.Now.AddDays(1)
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
        var testUser = CreateTestUser(UserRole.Driver);
        var testCar = CreateTestCar();
        var startTime = DateTime.Now;
        var endTime = DateTime.Now.AddDays(3);

        // Mock Cars DbSet
        var mockCars = CreateMockDbSet([testCar]);
        _mockContext.Setup(c => c.Cars).Returns(mockCars.Object);

        // Mock Bookings DbSet
        var mockBookings = CreateMockDbSet(new List<Booking>());
        _mockContext.Setup(c => c.Bookings).Returns(mockBookings.Object);

        // Setup SaveChanges
        _mockContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _currentUser.SetUser(testUser);

        var handler = new CreateBooking.Handler(_mockContext.Object, _currentUser);
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

        mockBookings.Verify(m => m.Add(It.IsAny<Booking>()), Times.Once);
        _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Validator_EndTimeBeforeStartTime_ReturnsValidationError()
    {
        // Arrange
        var validator = new CreateBooking.Validator();
        var command = new CreateBooking.CreateBookingCommand(
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            DateTime.Now,
            DateTime.Now.AddDays(-1)
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
    public void Validator_MissingRequiredFields_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new CreateBooking.Validator();
        var command = new CreateBooking.CreateBookingCommand(
            Guid.Empty,
            Guid.Empty,
            DateTime.MinValue,
            DateTime.MinValue
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "User không được để trống");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Car không được để trống");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Phải chọn thời gian bắt đầu thuê");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Phải chọn thời gian kết thúc thuê");
    }

    private static Booking CreateTestBooking(
        Guid userId,
        Guid carId,
        DateTime startTime,
        DateTime endTime,
        Car car
    )
    {
        var totalBookingDays = (endTime - startTime).Days;
        var basePrice = car.PricePerDay * totalBookingDays;
        var platformFee = basePrice * 0.1m;
        var totalAmount = basePrice + platformFee;

        return new Booking
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            UserId = userId,
            CarId = carId,
            StartTime = startTime,
            EndTime = endTime,
            ActualReturnTime = endTime,
            BasePrice = basePrice,
            PlatformFee = platformFee,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = totalAmount,
            Note = string.Empty // Required field
        };
    }

    [Fact]
    public async Task Handle_SameUserOverlappingBooking_ReturnsConflict()
    {
        // Arrange
        var testUser = CreateTestUser(UserRole.Driver);
        _currentUser.SetUser(testUser);
        var testCar = CreateTestCar();

        // Create existing booking with required fields
        var existingBooking = CreateTestBooking(
            userId: testUser.Id,
            carId: testCar.Id,
            startTime: DateTime.Now.AddHours(1),
            endTime: DateTime.Now.AddHours(3),
            car: testCar // Use the test car to calculate prices
        );

        // Mock Cars and Bookings
        var mockCars = CreateMockDbSet([testCar]);
        var mockBookings = CreateMockDbSet([existingBooking]);
        _mockContext.Setup(c => c.Cars).Returns(mockCars.Object);
        _mockContext.Setup(c => c.Bookings).Returns(mockBookings.Object);

        // Create new overlapping command
        var command = new CreateBooking.CreateBookingCommand(
            testUser.Id,
            testCar.Id,
            DateTime.Now.AddHours(2), // Overlaps
            DateTime.Now.AddHours(4)
        );

        var handler = new CreateBooking.Handler(_mockContext.Object, _currentUser);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Handle_DifferentUserOverlappingBooking_CreatesSuccessfully()
    {
        // Arrange
        var testUser1 = CreateTestUser(UserRole.Driver);
        var testUser2 = CreateTestUser(UserRole.Driver); // Different user
        var testCar = CreateTestCar();

        // Create existing booking with required fields
        var existingBooking = CreateTestBooking(
            userId: testUser1.Id,
            carId: testCar.Id,
            startTime: DateTime.Now.AddHours(1),
            endTime: DateTime.Now.AddHours(3),
            car: testCar // Use the test car to calculate prices
        );

        // Mock Cars and Bookings
        var mockCars = CreateMockDbSet([testCar]);
        var mockBookings = CreateMockDbSet([existingBooking]);
        _mockContext.Setup(c => c.Cars).Returns(mockCars.Object);
        _mockContext.Setup(c => c.Bookings).Returns(mockBookings.Object);

        // Use User2 for the new booking
        _currentUser.SetUser(testUser2);
        var handler = new CreateBooking.Handler(_mockContext.Object, _currentUser);
        var command = new CreateBooking.CreateBookingCommand(
            testUser2.Id,
            testCar.Id,
            DateTime.Now.AddHours(2), // Overlaps with User1's booking
            DateTime.Now.AddHours(4)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status); // Should succeed
    }
}
