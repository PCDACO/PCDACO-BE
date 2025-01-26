using Ardalis.Result;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.UC_Booking.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Booking.Commands;

public class CreateBookingTests : DatabaseTestBase
{
    [Fact]
    public async Task Handle_UserNotDriver_ReturnsError()
    {
        // Arrange
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Admin);
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
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Driver);
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
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Driver);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testCar = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            testUser.Id,
            testManufacturer.Id
        );
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
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Driver);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testCar = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            testUser.Id,
            testManufacturer.Id
        );
        _currentUser.SetUser(testUser);

        // Create overlapping booking
        var command1 = new CreateBooking.CreateBookingCommand(
            testUser.Id,
            testCar.Id,
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(3)
        );

        // Overlapping booking
        var command2 = new CreateBooking.CreateBookingCommand(
            testUser.Id,
            testCar.Id,
            DateTime.UtcNow.AddHours(2), // Overlaps
            DateTime.UtcNow.AddHours(4)
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
        var testUser1 = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Driver);
        var testUser2 = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Driver);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testCar = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            testUser1.Id,
            testManufacturer.Id
        );

        // Use users 2
        _currentUser.SetUser(testUser2);
        var handler = new CreateBooking.Handler(_dbContext, _currentUser);

        var command1 = new CreateBooking.CreateBookingCommand(
            testUser1.Id,
            testCar.Id,
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(3)
        );

        var command2 = new CreateBooking.CreateBookingCommand(
            testUser2.Id,
            testCar.Id,
            DateTime.UtcNow.AddHours(2), // Overlaps
            DateTime.UtcNow.AddHours(4)
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
