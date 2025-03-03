using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_Feedback.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Feedback.Queries;

[Collection("Test Collection")]
public class GetAllFeedbackForCurrentUserTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotDriverOrOwner_ReturnsForbidden()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(admin);

        var handler = new GetAllFeedbackForCurrentUser.Handler(_dbContext, _currentUser);
        var query = new GetAllFeedbackForCurrentUser.Query(1, 10, string.Empty);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_DriverWithNoFeedbacks_ReturnsEmptyList()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(driver);

        var handler = new GetAllFeedbackForCurrentUser.Handler(_dbContext, _currentUser);
        var query = new GetAllFeedbackForCurrentUser.Query(1, 10, string.Empty);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);
    }

    [Fact]
    public async Task Handle_OwnerWithNoFeedbacks_ReturnsEmptyList()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var handler = new GetAllFeedbackForCurrentUser.Handler(_dbContext, _currentUser);
        var query = new GetAllFeedbackForCurrentUser.Query(1, 10, string.Empty);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);
    }

    [Fact]
    public async Task Handle_DriverWithFeedbacks_ReturnsFeedbackList()
    {
        // Arrange
        // Create driver and owner roles
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        // Create driver and owner
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com",
            "John Doea",
            "0987654324"
        );
        _currentUser.SetUser(driver);

        // Create car for owner
        var car = await CreateTestCar(owner.Id);

        // Create booking
        var booking = await CreateTestBooking(driver.Id, car.Id);

        // Create feedback from owner to driver (driver should see this)
        var ownerFeedback = new Feedback
        {
            UserId = owner.Id,
            BookingId = booking.Id,
            Point = 4,
            Content = "Great driver",
            Type = FeedbackTypeEnum.Owner,
        };

        // Create feedback from driver to owner (driver should NOT see this)
        var driverFeedback = new Feedback
        {
            UserId = driver.Id,
            BookingId = booking.Id,
            Point = 5,
            Content = "Nice car",
            Type = FeedbackTypeEnum.Driver,
        };

        _dbContext.Feedbacks.AddRange(ownerFeedback, driverFeedback);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllFeedbackForCurrentUser.Handler(_dbContext, _currentUser);
        var query = new GetAllFeedbackForCurrentUser.Query(1, 10, string.Empty);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);

        var feedback = result.Value.Items.First();
        Assert.Equal(ownerFeedback.Id, feedback.Id);
        Assert.Equal(4, feedback.Rating);
        Assert.Equal("Great driver", feedback.Content);
        Assert.Equal(owner.Name, feedback.FromUserName);
        Assert.Equal(FeedbackTypeEnum.Owner, feedback.Type);
    }

    [Fact]
    public async Task Handle_OwnerWithFeedbacks_ReturnsFeedbackList()
    {
        // Arrange
        // Create driver and owner roles
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        // Create driver and owner
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com",
            "John Dea",
            "0987654323"
        );
        _currentUser.SetUser(owner);

        // Create car for owner
        var car = await CreateTestCar(owner.Id);

        // Create booking
        var booking = await CreateTestBooking(driver.Id, car.Id);

        // Create feedback from owner to driver (owner should NOT see this)
        var ownerFeedback = new Feedback
        {
            UserId = owner.Id,
            BookingId = booking.Id,
            Point = 4,
            Content = "Great driver",
            Type = FeedbackTypeEnum.Owner,
        };

        // Create feedback from driver to owner (owner should see this)
        var driverFeedback = new Feedback
        {
            UserId = driver.Id,
            BookingId = booking.Id,
            Point = 5,
            Content = "Nice car",
            Type = FeedbackTypeEnum.Driver,
        };

        _dbContext.Feedbacks.AddRange(ownerFeedback, driverFeedback);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllFeedbackForCurrentUser.Handler(_dbContext, _currentUser);
        var query = new GetAllFeedbackForCurrentUser.Query(1, 10, string.Empty);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);

        var feedback = result.Value.Items.First();
        Assert.Equal(driverFeedback.Id, feedback.Id);
        Assert.Equal(5, feedback.Rating);
        Assert.Equal("Nice car", feedback.Content);
        Assert.Equal(driver.Name, feedback.FromUserName);
        Assert.Equal(FeedbackTypeEnum.Driver, feedback.Type);
    }

    [Fact]
    public async Task Handle_WithKeywordFilter_ReturnsFilteredResults()
    {
        // Arrange
        // Create driver and owner roles
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        // Create driver and owner
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com",
            "Test Owner",
            "0987654322"
        );
        _currentUser.SetUser(driver);

        // Create car for owner
        var car = await CreateTestCar(owner.Id);

        // Create booking
        var booking = await CreateTestBooking(driver.Id, car.Id);

        // Create two feedbacks with different content
        var feedback1 = new Feedback
        {
            UserId = owner.Id,
            BookingId = booking.Id,
            Point = 4,
            Content = "Great experience with this driver",
            Type = FeedbackTypeEnum.Owner,
        };

        var feedback2 = new Feedback
        {
            UserId = owner.Id,
            BookingId = booking.Id,
            Point = 5,
            Content = "Terrible service",
            Type = FeedbackTypeEnum.Owner,
        };

        _dbContext.Feedbacks.AddRange(feedback1, feedback2);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllFeedbackForCurrentUser.Handler(_dbContext, _currentUser);
        var query = new GetAllFeedbackForCurrentUser.Query(1, 10, "Great");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal(1, result.Value.TotalItems);

        var feedback = result.Value.Items.First();
        Assert.Equal(feedback1.Id, feedback.Id);
        Assert.Equal("Great experience with this driver", feedback.Content);
    }

    [Fact]
    public async Task Handle_WithOwnerNameKeywordFilter_ReturnsFilteredResults()
    {
        // Arrange
        // Create driver and owner roles
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        // Create driver and two owners with different names
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var owner1 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner1@test.com",
            "John Smith",
            "1234567890"
        );
        var owner2 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner2@test.com",
            "Jane Doe",
            "0987654321"
        );
        _currentUser.SetUser(driver);

        // Create cars for owners
        var car1 = await CreateTestCar(owner1.Id);
        var car2 = await CreateTestCar(owner2.Id);

        // Create bookings
        var booking1 = await CreateTestBooking(driver.Id, car1.Id);
        var booking2 = await CreateTestBooking(driver.Id, car2.Id);

        // Create feedbacks from both owners
        var feedback1 = new Feedback
        {
            UserId = owner1.Id,
            BookingId = booking1.Id,
            Point = 4,
            Content = "Good driver",
            Type = FeedbackTypeEnum.Owner,
        };

        var feedback2 = new Feedback
        {
            UserId = owner2.Id,
            BookingId = booking2.Id,
            Point = 5,
            Content = "Great driver",
            Type = FeedbackTypeEnum.Owner,
        };

        _dbContext.Feedbacks.AddRange(feedback1, feedback2);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllFeedbackForCurrentUser.Handler(_dbContext, _currentUser);
        var query = new GetAllFeedbackForCurrentUser.Query(1, 10, "John");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal(1, result.Value.TotalItems);

        var feedback = result.Value.Items.First();
        Assert.Equal(feedback1.Id, feedback.Id);
        Assert.Equal("John Smith", feedback.FromUserName);
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        // Create driver and owner roles
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        // Create driver and owner
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com",
            "Doe lael",
            "0987654325"
        );
        _currentUser.SetUser(driver);

        // Create car for owner
        var car = await CreateTestCar(owner.Id);

        // Create booking
        var booking = await CreateTestBooking(driver.Id, car.Id);

        // Create 3 feedbacks
        var feedbacks = new List<Feedback>();
        for (int i = 0; i < 3; i++)
        {
            var feedback = new Feedback
            {
                UserId = owner.Id,
                BookingId = booking.Id,
                Point = 4 + i % 2, // Alternating between 4 and 5
                Content = $"Feedback #{i + 1}",
                Type = FeedbackTypeEnum.Owner,
            };
            feedbacks.Add(feedback);
        }

        _dbContext.Feedbacks.AddRange(feedbacks);
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllFeedbackForCurrentUser.Handler(_dbContext, _currentUser);

        // Act - First Page (2 items per page)
        var queryPage1 = new GetAllFeedbackForCurrentUser.Query(1, 2, string.Empty);
        var resultPage1 = await handler.Handle(queryPage1, CancellationToken.None);

        // Act - Second Page (2 items per page)
        var queryPage2 = new GetAllFeedbackForCurrentUser.Query(2, 2, string.Empty);
        var resultPage2 = await handler.Handle(queryPage2, CancellationToken.None);

        // Assert - First Page
        Assert.Equal(ResultStatus.Ok, resultPage1.Status);
        Assert.Equal(2, resultPage1.Value.Items.Count());
        Assert.Equal(3, resultPage1.Value.TotalItems);

        // Assert - Second Page
        Assert.Equal(ResultStatus.Ok, resultPage2.Status);
        Assert.Single(resultPage2.Value.Items);
        Assert.Equal(3, resultPage2.Value.TotalItems);
    }

    // Helper methods to create necessary entities for testing
    private async Task<Car> CreateTestCar(Guid ownerId)
    {
        // Create prerequisites
        var statusId = await CreateTestCarStatus();
        var modelId = await CreateTestCarModel();
        var fuelTypeId = await CreateTestFuelType();
        var transmissionTypeId = await CreateTestTransmissionType();
        var encryptionKeyId = await CreateTestEncryptionKey();

        // Create car
        var car = new Car
        {
            OwnerId = ownerId,
            ModelId = modelId,
            StatusId = statusId,
            FuelTypeId = fuelTypeId,
            TransmissionTypeId = transmissionTypeId,
            EncryptionKeyId = encryptionKeyId,
            EncryptedLicensePlate = "encrypted_license_plate",
            Color = "Red",
            Seat = 4,
            FuelConsumption = 7.5m,
            Price = 50.0m,
        };

        _dbContext.Cars.Add(car);
        await _dbContext.SaveChangesAsync();

        return car;
    }

    private async Task<Guid> CreateTestCarStatus()
    {
        var status = new CarStatus { Name = "Available" };
        _dbContext.CarStatuses.Add(status);
        await _dbContext.SaveChangesAsync();
        return status.Id;
    }

    private async Task<Guid> CreateTestCarModel()
    {
        var manufacturer = new Manufacturer { Name = "Toyota" };
        _dbContext.Manufacturers.Add(manufacturer);
        await _dbContext.SaveChangesAsync();

        var model = new Model
        {
            Name = "Camry",
            ManufacturerId = manufacturer.Id,
            ReleaseDate = DateTimeOffset.UtcNow,
        };
        _dbContext.Models.Add(model);
        await _dbContext.SaveChangesAsync();

        return model.Id;
    }

    private async Task<Guid> CreateTestFuelType()
    {
        var fuelType = new FuelType { Name = "Gasoline" };
        _dbContext.FuelTypes.Add(fuelType);
        await _dbContext.SaveChangesAsync();
        return fuelType.Id;
    }

    private async Task<Guid> CreateTestTransmissionType()
    {
        var transmissionType = new TransmissionType { Name = "Automatic" };
        _dbContext.TransmissionTypes.Add(transmissionType);
        await _dbContext.SaveChangesAsync();
        return transmissionType.Id;
    }

    private async Task<Guid> CreateTestEncryptionKey()
    {
        var encryptionKey = new EncryptionKey { EncryptedKey = "test_key", IV = "test_iv" };
        _dbContext.EncryptionKeys.Add(encryptionKey);
        await _dbContext.SaveChangesAsync();
        return encryptionKey.Id;
    }

    private async Task<Booking> CreateTestBooking(Guid userId, Guid carId)
    {
        // Create booking status
        var bookingStatus = new BookingStatus { Name = BookingStatusEnum.Completed.ToString() };
        _dbContext.BookingStatuses.Add(bookingStatus);
        await _dbContext.SaveChangesAsync();

        // Create booking
        var booking = new Booking
        {
            UserId = userId,
            CarId = carId,
            StatusId = bookingStatus.Id,
            StartTime = DateTimeOffset.UtcNow.AddDays(-7),
            EndTime = DateTimeOffset.UtcNow.AddDays(-6),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(-6),
            BasePrice = 100.0m,
            PlatformFee = 20.0m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 120.0m,
            Note = "Test booking",
        };

        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        return booking;
    }
}
