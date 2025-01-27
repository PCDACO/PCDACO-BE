using Ardalis.Result;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_Booking.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Booking.Commands;

[Collection("Test Collection")]
public class CreateBookingTests(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotDriver_ReturnsError()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        BookingStatus bookingStatus = await TestDataBookingStatus.CreateTestBookingStatus(
            _dbContext,
            "Pending"
        );

        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new CreateBooking.Handler(_dbContext, _currentUser);
        var command = new CreateBooking.CreateBookingCommand(
            UserId: testUser.Id,
            CarId: Uuid.NewDatabaseFriendly(Database.PostgreSql),
            StatusId: bookingStatus.Id,
            StartTime: DateTime.UtcNow,
            EndTime: DateTime.UtcNow.AddDays(1)
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
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        BookingStatus bookingStatus = await TestDataBookingStatus.CreateTestBookingStatus(
            _dbContext,
            "Pending"
        );

        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new CreateBooking.Handler(_dbContext, _currentUser);
        var command = new CreateBooking.CreateBookingCommand(
            UserId: testUser.Id,
            CarId: Uuid.NewDatabaseFriendly(Database.PostgreSql),
            StatusId: bookingStatus.Id,
            StartTime: DateTime.UtcNow,
            EndTime: DateTime.UtcNow.AddDays(1)
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
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        BookingStatus bookingStatus = await TestDataBookingStatus.CreateTestBookingStatus(
            _dbContext,
            "Pending"
        );
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        CarStatus carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");

        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: testUser.Id,
            manufacturerId: testManufacturer.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: carStatus
        );
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow.AddDays(3);

        _currentUser.SetUser(testUser);

        var handler = new CreateBooking.Handler(_dbContext, _currentUser);
        var command = new CreateBooking.CreateBookingCommand(
            UserId: testUser.Id,
            CarId: testCar.Id,
            StatusId: bookingStatus.Id,
            StartTime: startTime,
            EndTime: endTime
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
    public async Task Validator_EndTimeBeforeStartTime_ReturnsValidationError()
    {
        // Arrange
        BookingStatus bookingStatus = await TestDataBookingStatus.CreateTestBookingStatus(
            _dbContext,
            "Pending"
        );
        var validator = new CreateBooking.Validator();
        var command = new CreateBooking.CreateBookingCommand(
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            bookingStatus.Id,
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
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        CarStatus carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");

        BookingStatus bookingStatus = await TestDataBookingStatus.CreateTestBookingStatus(
            _dbContext,
            "Pending"
        );

        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: testUser.Id,
            manufacturerId: testManufacturer.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: carStatus
        );
        _currentUser.SetUser(testUser);

        // Create overlapping booking
        var command1 = new CreateBooking.CreateBookingCommand(
            UserId: testUser.Id,
            CarId: testCar.Id,
            StatusId: bookingStatus.Id,
            StartTime: DateTime.UtcNow.AddHours(1),
            EndTime: DateTime.UtcNow.AddHours(3)
        );

        // Overlapping booking
        var command2 = new CreateBooking.CreateBookingCommand(
            UserId: testUser.Id,
            CarId: testCar.Id,
            StatusId: bookingStatus.Id,
            StartTime: DateTime.UtcNow.AddHours(2), // Overlaps
            EndTime: DateTime.UtcNow.AddHours(4)
        );

        var handler = new CreateBooking.Handler(_dbContext, _currentUser);

        // Act
        var result1 = await handler.Handle(command1, CancellationToken.None);
        var result2 = await handler.Handle(command2, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result2.Status);
    }

    [Fact]
    public async Task Handle_DifferentUserOverlappingBooking_CreatesSuccessfully()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        CarStatus carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");

        BookingStatus bookingStatus = await TestDataBookingStatus.CreateTestBookingStatus(
            _dbContext,
            "Pending"
        );

        var testUser1 = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var testUser2 = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: testUser1.Id,
            manufacturerId: testManufacturer.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: carStatus
        );

        // Use users 2
        _currentUser.SetUser(testUser2);
        var handler = new CreateBooking.Handler(_dbContext, _currentUser);

        var command1 = new CreateBooking.CreateBookingCommand(
            UserId: testUser1.Id,
            CarId: testCar.Id,
            StatusId: bookingStatus.Id,
            StartTime: DateTime.UtcNow.AddHours(1),
            EndTime: DateTime.UtcNow.AddHours(3)
        );

        var command2 = new CreateBooking.CreateBookingCommand(
            UserId: testUser2.Id,
            CarId: testCar.Id,
            StatusId: bookingStatus.Id,
            StartTime: DateTime.UtcNow.AddHours(2), // Overlaps
            EndTime: DateTime.UtcNow.AddHours(4)
        );

        // Act
        var result1 = await handler.Handle(command1, CancellationToken.None);
        var result2 = await handler.Handle(command2, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result2.Status);

        var newBooking = await _dbContext.Bookings.FirstOrDefaultAsync(b =>
            b.UserId == testUser2.Id
        );
        Assert.NotNull(newBooking);
    }
}
