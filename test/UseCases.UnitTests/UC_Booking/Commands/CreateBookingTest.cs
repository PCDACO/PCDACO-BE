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
            Id = Guid.NewGuid(),
            EncryptionKeyId = Guid.NewGuid(),
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
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            ManufacturerId = Guid.NewGuid(),
            EncryptionKeyId = Guid.NewGuid(),
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
        var mockSet = data.BuildMock().BuildMockDbSet();

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
            Guid.NewGuid(),
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
            Guid.NewGuid(),
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
            Guid.NewGuid(),
            Guid.NewGuid(),
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
}
