using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_Feedback.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Feedback.Queries;

[Collection("Test Collection")]
public class GetAllFeedbacksByBookingIdTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_BookingNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new GetAllFeedbacksByBookingId.Handler(_dbContext, _currentUser);
        var query = new GetAllFeedbacksByBookingId.Query(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("Không tìm thấy đơn đặt xe", result.Errors.First());
    }

    [Fact]
    public async Task Handle_UnauthorizedAccess_ReturnsForbidden()
    {
        // Arrange
        UserRole randomRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Random");
        var unauthorizedUser = await TestDataCreateUser.CreateTestUser(_dbContext, randomRole);
        _currentUser.SetUser(unauthorizedUser);

        // Create owner and driver
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);

        // Create booking
        var booking = await CreateTestBooking(driver.Id, owner.Id);

        var handler = new GetAllFeedbacksByBookingId.Handler(_dbContext, _currentUser);
        var query = new GetAllFeedbacksByBookingId.Query(booking.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(ResponseMessages.ForbiddenAudit, result.Errors.First());
    }

    [Fact]
    public async Task Handle_NoFeedbacks_ReturnsEmptyList()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(driver);

        var booking = await CreateTestBooking(driver.Id, driver.Id); // Using driver as owner for simplicity

        var handler = new GetAllFeedbacksByBookingId.Handler(_dbContext, _currentUser);
        var query = new GetAllFeedbacksByBookingId.Query(booking.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
    }

    [Fact]
    public async Task Handle_WithFeedbacks_ReturnsFeedbackList()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com",
            "John Smith"
        );
        _currentUser.SetUser(driver);

        var booking = await CreateTestBooking(driver.Id, owner.Id);

        // Create feedback from owner to driver
        var ownerFeedback = new Feedback
        {
            UserId = owner.Id,
            BookingId = booking.Id,
            Point = 4,
            Content = "Great driver",
            Type = FeedbackTypeEnum.ToDriver,
        };

        // Create feedback from driver to owner
        var driverFeedback = new Feedback
        {
            UserId = driver.Id,
            BookingId = booking.Id,
            Point = 5,
            Content = "Nice car",
            Type = FeedbackTypeEnum.ToOwner,
        };

        _dbContext.Feedbacks.AddRange(ownerFeedback, driverFeedback);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllFeedbacksByBookingId.Handler(_dbContext, _currentUser);
        var query = new GetAllFeedbacksByBookingId.Query(booking.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.Items.Count());
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);
    }

    [Fact]
    public async Task Handle_WithKeywordFilter_ReturnsFilteredResults()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com",
            "John Smith",
            "1234567890"
        );
        _currentUser.SetUser(driver);

        var booking = await CreateTestBooking(driver.Id, owner.Id);

        // Create feedbacks with different content
        var feedback1 = new Feedback
        {
            UserId = owner.Id,
            BookingId = booking.Id,
            Point = 4,
            Content = "Great experience with this driver",
            Type = FeedbackTypeEnum.ToDriver,
        };

        var feedback2 = new Feedback
        {
            UserId = driver.Id,
            BookingId = booking.Id,
            Point = 5,
            Content = "Excellent car condition",
            Type = FeedbackTypeEnum.ToOwner,
        };

        _dbContext.Feedbacks.AddRange(feedback1, feedback2);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllFeedbacksByBookingId.Handler(_dbContext, _currentUser);
        var query = new GetAllFeedbacksByBookingId.Query(booking.Id, Keyword: "Great");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        var feedback = result.Value.Items.First();
        Assert.Equal(feedback1.Id, feedback.Id);
        Assert.Contains("Great", feedback.Content);
    }

    // Helper method to create a test booking
    private async Task<Booking> CreateTestBooking(Guid driverId, Guid ownerId)
    {
        // Create car prerequisites
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create car using TestDataCreateCar helper
        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            ownerId,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Available
        );

        // Create booking
        var booking = new Booking
        {
            UserId = driverId,
            CarId = car.Id,
            Status = BookingStatusEnum.Completed,
            StartTime = DateTimeOffset.UtcNow.AddDays(-2),
            EndTime = DateTimeOffset.UtcNow.AddDays(-1),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(-1),
            BasePrice = 100.0m,
            PlatformFee = 10.0m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 110.0m,
            Note = "Test booking",
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        return booking;
    }
}
