using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_Feedback.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Feedback.Queries;

[Collection("Test Collection")]
public class CreateFeedbackTests : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly Func<Task> _resetDatabase;
    private readonly CreateFeedBack.Handler _handler;
    private readonly CreateFeedBack.Validator _validator;

    public CreateFeedbackTests(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _currentUser = fixture.CurrentUser;
        _resetDatabase = fixture.ResetDatabaseAsync;
        _handler = new CreateFeedBack.Handler(_dbContext, _currentUser);
        _validator = new CreateFeedBack.Validator();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UnauthorizedUser_ReturnsForbidden()
    {
        // Arrange
        _currentUser.SetUser(null!);
        var command = new CreateFeedBack.Command(Guid.NewGuid(), 5, "Great service");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_BookingNotFound_ReturnsError()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);

        var command = new CreateFeedBack.Command(Guid.NewGuid(), 5, "Great service");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Không tìm thấy booking", result.Errors);
    }

    [Fact]
    public async Task Handle_DriverNotMatchBooking_ReturnsError()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var wrongDriver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(wrongDriver);

        var testBookingResult = await CreateTestBooking(BookingStatusEnum.Completed);

        var command = new CreateFeedBack.Command(testBookingResult.booking.Id, 5, "Great service");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Chỉ người thuê xe mới có thể đánh giá chủ xe", result.Errors);
    }

    [Fact]
    public async Task Handle_OwnerNotMatchCar_ReturnsError()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var wrongOwner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(wrongOwner);

        var testBookingResult = await CreateTestBooking(BookingStatusEnum.Completed);

        var command = new CreateFeedBack.Command(testBookingResult.booking.Id, 5, "Great driver");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Chỉ chủ xe mới có thể đánh giá người thuê", result.Errors);
    }

    [Fact]
    public async Task Handle_OutsideFeedbackPeriod_ReturnsError()
    {
        // Arrange
        var testBookingResult = await CreateTestBooking(BookingStatusEnum.Completed, daysAgo: 8);
        _currentUser.SetUser(testBookingResult.owner);

        var command = new CreateFeedBack.Command(testBookingResult.booking.Id, 5, "Great service");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Chỉ có thể đánh giá trong vòng 7 ngày", result.Errors.First());
    }

    [Fact]
    public async Task Handle_DuplicateFeedback_ReturnsError()
    {
        // Arrange
        var testBookingResult = await CreateTestBooking(BookingStatusEnum.Completed);
        _currentUser.SetUser(testBookingResult.driver);

        // Create existing feedback
        var existingFeedback = new Feedback
        {
            BookingId = testBookingResult.booking.Id,
            UserId = testBookingResult.driver.Id,
            Point = 5,
            Content = "Existing feedback",
            Type = FeedbackTypeEnum.Driver,
        };
        _dbContext.Feedbacks.Add(existingFeedback);
        await _dbContext.SaveChangesAsync();

        var command = new CreateFeedBack.Command(
            testBookingResult.booking.Id,
            5,
            "Another feedback"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Feedback đã tồn tại", result.Errors);
    }

    [Fact]
    public async Task Handle_BookingNotCompleted_ReturnsError()
    {
        // Arrange
        var testBookingData = await CreateTestBooking(BookingStatusEnum.Ongoing);

        _currentUser.SetUser(testBookingData.driver);

        var command = new CreateFeedBack.Command(testBookingData.booking.Id, 5, "Great service");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Chỉ có thể tạo feedback khi chuyến đi đã hoàn thành", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidDriverFeedback_CreatesSuccessfully()
    {
        // Arrange
        var testBookingResult = await CreateTestBooking(BookingStatusEnum.Completed);
        _currentUser.SetUser(testBookingResult.driver);

        var command = new CreateFeedBack.Command(testBookingResult.booking.Id, 5, "Great service");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Tạo feedback thành công", result.SuccessMessage);

        var feedback = await _dbContext.Feedbacks.FirstOrDefaultAsync(f =>
            f.BookingId == testBookingResult.booking.Id
        );
        Assert.NotNull(feedback);
        Assert.Equal(FeedbackTypeEnum.Driver, feedback.Type);
        Assert.Equal(5, feedback.Point);
        Assert.Equal("Great service", feedback.Content);
    }

    // Helper method to create test booking
    private async Task<(Booking booking, User owner, User driver)> CreateTestBooking(
        BookingStatusEnum statusEnum,
        int daysAgo = 1
    )
    {
        // Create owner and car
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Gasoline");

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            carStatus
        );

        // Create booking status
        var status = new BookingStatus { Name = statusEnum.ToString() };
        _dbContext.BookingStatuses.Add(status);
        await _dbContext.SaveChangesAsync();

        // Create driver
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);

        // Create booking
        var booking = new Booking
        {
            UserId = driver.Id,
            CarId = car.Id,
            StatusId = status.Id,
            StartTime = DateTimeOffset.UtcNow.AddDays(-daysAgo - 1),
            EndTime = DateTimeOffset.UtcNow.AddDays(-daysAgo),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(-daysAgo),
            BasePrice = 100.0m,
            PlatformFee = 10.0m,
            TotalAmount = 110.0m,
            Note = "Test booking",
            ExcessDay = 0,
            ExcessDayFee = 0,
        };

        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        return (booking, owner, driver);
    }

    [Fact]
    public void Validator_EmptyContent_ReturnsError()
    {
        // Arrange
        var command = new CreateFeedBack.Command(
            BookingId: Guid.NewGuid(),
            Rating: 5,
            Content: string.Empty
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage == "Nội dung feedback không được để trống"
        );
    }

    [Fact]
    public void Validator_ContentTooLong_ReturnsError()
    {
        // Arrange
        var command = new CreateFeedBack.Command(
            BookingId: Guid.NewGuid(),
            Rating: 5,
            Content: new string('x', 501) // Create string longer than 500 chars
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage == "Nội dung feedback không được vượt quá 500 ký tự"
        );
    }

    [Theory]
    [InlineData(0, "Điểm đánh giá phải nằm trong khoảng từ 1 đến 5")]
    [InlineData(6, "Điểm đánh giá phải nằm trong khoảng từ 1 đến 5")]
    public void Validator_InvalidRating_ReturnsError(int rating, string expectedError)
    {
        // Arrange
        var command = new CreateFeedBack.Command(
            BookingId: Guid.NewGuid(),
            Rating: rating,
            Content: "Valid content"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == expectedError);
    }

    [Fact]
    public void Validator_ValidRequest_PassesValidation()
    {
        // Arrange
        var command = new CreateFeedBack.Command(
            BookingId: Guid.NewGuid(),
            Rating: 5,
            Content: "Valid feedback content"
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
