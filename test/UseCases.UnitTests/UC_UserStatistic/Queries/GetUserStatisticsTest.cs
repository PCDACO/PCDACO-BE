using Ardalis.Result;
using Domain.Constants;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_UserStatistic.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_UserStatistic.Queries;

[Collection("Test Collection")]
public class GetUserStatisticsTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly KeyManagementService keyManagementService = fixture.KeyManagementService;
    private readonly AesEncryptionService aesEncryptionService = fixture.AesEncryptionService;
    private readonly EncryptionSettings encryptionSettings = fixture.EncryptionSettings;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        user.IsDeleted = true;
        await _dbContext.SaveChangesAsync();
        _currentUser.SetUser(user); // Non-existent user
        var handler = new GetUserStatistics.Handler(_dbContext, _currentUser);
        var query = new GetUserStatistics.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.UserNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_DriverRole_ReturnsCorrectStatistics()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(driver);

        // Create booking statuses
        var completedStatus = await CreateBookingStatus(BookingStatusEnum.Completed.ToString());
        var rejectedStatus = await CreateBookingStatus(BookingStatusEnum.Rejected.ToString());
        var expiredStatus = await CreateBookingStatus(BookingStatusEnum.Expired.ToString());
        var cancelledStatus = await CreateBookingStatus(BookingStatusEnum.Cancelled.ToString());
        var pendingStatus = await CreateBookingStatus(BookingStatusEnum.Pending.ToString());

        // Create owner and car for bookings
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com"
        );
        var car = await CreateTestCar(owner.Id, _dbContext);

        // Create bookings with different statuses
        await CreateBooking(driver.Id, car.Id, completedStatus.Id); // Completed
        await CreateBooking(driver.Id, car.Id, completedStatus.Id); // Completed
        await CreateBooking(driver.Id, car.Id, rejectedStatus.Id); // Rejected
        await CreateBooking(driver.Id, car.Id, expiredStatus.Id); // Expired
        await CreateBooking(driver.Id, car.Id, cancelledStatus.Id); // Cancelled
        await CreateBooking(driver.Id, car.Id, pendingStatus.Id); // Pending

        // Create feedback for the completed booking
        var booking = await _dbContext
            .Bookings.Where(b =>
                b.UserId == driver.Id && b.Status.Name == BookingStatusEnum.Completed.ToString()
            )
            .FirstAsync();

        await CreateFeedback(booking.Id, FeedbackTypeEnum.Owner, 4, _dbContext);
        await CreateFeedback(booking.Id, FeedbackTypeEnum.Owner, 5, _dbContext);

        var handler = new GetUserStatistics.Handler(_dbContext, _currentUser);
        var query = new GetUserStatistics.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);

        var response = result.Value;
        Assert.Equal(6, response.TotalBooking); // Total of all bookings
        Assert.Equal(2, response.TotalCompleted);
        Assert.Equal(1, response.TotalRejected);
        Assert.Equal(1, response.TotalExpired);
        Assert.Equal(1, response.TotalCancelled);
        Assert.Equal(0, response.TotalEarning); // Drivers don't earn money
        Assert.Equal(4.5m, response.AverageRating); // Average of 4 and 5
        Assert.Equal(0, response.TotalCreatedInspectionSchedule);
        Assert.Equal(0, response.TotalApprovedInspectionSchedule);
        Assert.Equal(0, response.TotalRejectedInspectionSchedule);
    }

    [Fact]
    public async Task Handle_OwnerRole_ReturnsCorrectStatistics()
    {
        // Arrange
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        // Create booking statuses
        var completedStatus = await CreateBookingStatus(BookingStatusEnum.Completed.ToString());

        // Create driver for bookings
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );

        // Create car for the owner
        var car = await CreateTestCar(owner.Id, _dbContext);

        // Create completed bookings
        var booking1 = await CreateBooking(driver.Id, car.Id, completedStatus.Id, 500);
        var booking2 = await CreateBooking(driver.Id, car.Id, completedStatus.Id, 750);

        // Create feedback for the completed bookings
        await CreateFeedback(booking1.Id, FeedbackTypeEnum.Driver, 3, _dbContext);
        await CreateFeedback(booking2.Id, FeedbackTypeEnum.Driver, 5, _dbContext);

        var handler = new GetUserStatistics.Handler(_dbContext, _currentUser);
        var query = new GetUserStatistics.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        var response = result.Value;
        Assert.Equal(0, response.TotalBooking);
        Assert.Equal(0, response.TotalCompleted);
        Assert.Equal(0, response.TotalRejected);
        Assert.Equal(0, response.TotalExpired);
        Assert.Equal(0, response.TotalCancelled);
        Assert.Equal(1250, response.TotalEarning); // Sum of 500 + 750 (two bookings from car)
        Assert.Equal(4, response.AverageRating); // Average of 3 and 5 (two feedbacks)
        Assert.Equal(0, response.TotalCreatedInspectionSchedule);
        Assert.Equal(0, response.TotalApprovedInspectionSchedule);
        Assert.Equal(0, response.TotalRejectedInspectionSchedule);
    }

    [Fact]
    public async Task Handle_ConsultantRole_ReturnsCorrectScheduleStatistics()
    {
        // Arrange
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);
        _currentUser.SetUser(consultant);

        // Create inspection status
        var pendingStatus = await CreateInspectionStatus(InspectionStatusNames.Pending);

        // Create car and technician for the schedule
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com"
        );
        var car = await CreateTestCar(owner.Id, _dbContext);

        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@test.com"
        );

        // Create inspection schedules
        await CreateInspectionSchedule(car.Id, technician.Id, pendingStatus.Id, consultant.Id);
        await CreateInspectionSchedule(car.Id, technician.Id, pendingStatus.Id, consultant.Id);
        await CreateInspectionSchedule(car.Id, technician.Id, pendingStatus.Id, consultant.Id);

        var handler = new GetUserStatistics.Handler(_dbContext, _currentUser);
        var query = new GetUserStatistics.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        var response = result.Value;
        Assert.Equal(0, response.TotalBooking);
        Assert.Equal(0, response.TotalCompleted);
        Assert.Equal(0, response.TotalRejected);
        Assert.Equal(0, response.TotalExpired);
        Assert.Equal(0, response.TotalCancelled);
        Assert.Equal(0, response.TotalEarning);
        Assert.Equal(0, response.AverageRating);
        Assert.Equal(3, response.TotalCreatedInspectionSchedule); // Created 3 schedules
        Assert.Equal(0, response.TotalApprovedInspectionSchedule); // Consultants don't approve schedules
        Assert.Equal(0, response.TotalRejectedInspectionSchedule); // Consultants don't reject schedules
    }

    [Fact]
    public async Task Handle_TechnicianRole_ReturnsCorrectScheduleStatistics()
    {
        // Arrange
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);
        _currentUser.SetUser(technician);

        // Create inspection statuses
        var approvedStatus = await CreateInspectionStatus(InspectionStatusNames.Approved);
        var rejectedStatus = await CreateInspectionStatus(InspectionStatusNames.Rejected);
        var pendingStatus = await CreateInspectionStatus(InspectionStatusNames.Pending);

        // Create consultant, car and owner for the schedule
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var consultant = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            consultantRole,
            "consultant@test.com"
        );

        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "owner@test.com"
        );
        var car = await CreateTestCar(owner.Id, _dbContext);

        // Create inspection schedules
        await CreateInspectionSchedule(car.Id, technician.Id, approvedStatus.Id, consultant.Id);
        await CreateInspectionSchedule(car.Id, technician.Id, approvedStatus.Id, consultant.Id);
        await CreateInspectionSchedule(car.Id, technician.Id, rejectedStatus.Id, consultant.Id);
        await CreateInspectionSchedule(car.Id, technician.Id, pendingStatus.Id, consultant.Id);

        var handler = new GetUserStatistics.Handler(_dbContext, _currentUser);
        var query = new GetUserStatistics.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        var response = result.Value;
        Assert.Equal(0, response.TotalBooking);
        Assert.Equal(0, response.TotalCompleted);
        Assert.Equal(0, response.TotalRejected);
        Assert.Equal(0, response.TotalExpired);
        Assert.Equal(0, response.TotalCancelled);
        Assert.Equal(0, response.TotalEarning);
        Assert.Equal(0, response.AverageRating);
        Assert.Equal(0, response.TotalCreatedInspectionSchedule);
        Assert.Equal(2, response.TotalApprovedInspectionSchedule); // Approved 2 schedules
        Assert.Equal(1, response.TotalRejectedInspectionSchedule); // Rejected 1 schedule
    }

    [Fact]
    public async Task Handle_AdminRole_ReturnsEmptyStatistics()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(admin);

        var handler = new GetUserStatistics.Handler(_dbContext, _currentUser);
        var query = new GetUserStatistics.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        var response = result.Value;
        Assert.Equal(0, response.TotalBooking);
        Assert.Equal(0, response.TotalCompleted);
        Assert.Equal(0, response.TotalRejected);
        Assert.Equal(0, response.TotalExpired);
        Assert.Equal(0, response.TotalCancelled);
        Assert.Equal(0, response.TotalEarning);
        Assert.Equal(0, response.AverageRating);
        Assert.Equal(0, response.TotalCreatedInspectionSchedule);
        Assert.Equal(0, response.TotalApprovedInspectionSchedule);
        Assert.Equal(0, response.TotalRejectedInspectionSchedule);
    }

    // Helper methods
    private async Task<BookingStatus> CreateBookingStatus(string name)
    {
        var status = new BookingStatus { Name = name };
        await _dbContext.BookingStatuses.AddAsync(status);
        await _dbContext.SaveChangesAsync();
        return status;
    }

    private async Task<Car> CreateTestCar(Guid ownerId, AppDBContext context)
    {
        var carStatus = new CarStatus { Name = "Available" };
        await _dbContext.CarStatuses.AddAsync(carStatus);

        var manufacturer = new Manufacturer { Name = "Test Manufacturer" };
        await _dbContext.Manufacturers.AddAsync(manufacturer);

        var model = new Model
        {
            Name = "Test Model",
            ManufacturerId = manufacturer.Id,
            ReleaseDate = DateTimeOffset.UtcNow.AddDays(7),
        };
        await _dbContext.Models.AddAsync(model);

        var transmissionType = new TransmissionType { Name = "Automatic" };
        await _dbContext.TransmissionTypes.AddAsync(transmissionType);

        var fuelType = new FuelType { Name = "Gasoline" };
        await _dbContext.FuelTypes.AddAsync(fuelType);

        (string key, string iv) = await keyManagementService.GenerateKeyAsync();
        string encryptedLicensePlate = await aesEncryptionService.Encrypt(
            "TestLicensePlate",
            key,
            iv
        );
        string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);
        EncryptionKey newEncryptionKey = new() { EncryptedKey = encryptedKey, IV = iv };
        context.EncryptionKeys.Add(newEncryptionKey);

        var car = new Car
        {
            OwnerId = ownerId,
            StatusId = carStatus.Id,
            EncryptedLicensePlate = encryptedLicensePlate,
            ModelId = model.Id,
            TransmissionTypeId = transmissionType.Id,
            FuelTypeId = fuelType.Id,
            EncryptionKeyId = newEncryptionKey.Id,
            Color = "Red",
            Seat = 5,
            FuelConsumption = 7.5m,
            Price = 100,
            RequiresCollateral = false,
            Description = "Test car description",
            Terms = "Standard terms",
        };

        await _dbContext.Cars.AddAsync(car);

        var carStatistic = new CarStatistic { CarId = car.Id };
        await _dbContext.CarStatistics.AddAsync(carStatistic);

        await _dbContext.SaveChangesAsync();

        return car;
    }

    private async Task<Booking> CreateBooking(
        Guid userId,
        Guid carId,
        Guid statusId,
        decimal basePrice = 100
    )
    {
        var booking = new Booking
        {
            UserId = userId,
            CarId = carId,
            StatusId = statusId,
            StartTime = DateTimeOffset.UtcNow.AddDays(-2),
            EndTime = DateTimeOffset.UtcNow.AddDays(-1),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(-1),
            BasePrice = basePrice,
            PlatformFee = basePrice * 0.1m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = basePrice + (basePrice * 0.1m),
            Note = "Test booking note",
            IsCarReturned = true,
            IsPaid = false,
        };

        if (
            statusId
            == _dbContext
                .BookingStatuses.First(s => s.Name == BookingStatusEnum.Completed.ToString())
                .Id
        )
        {
            booking.IsCarReturned = true;
            booking.IsPaid = true;
        }

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        return booking;
    }

    private async Task<Feedback> CreateFeedback(
        Guid bookingId,
        FeedbackTypeEnum type,
        int rating,
        AppDBContext context
    )
    {
        // Get the booking to retrieve related information
        var booking = await _dbContext.Bookings.FirstAsync(b => b.Id == bookingId);

        // Find a user with matching role based on feedback type
        string roleName = type.ToString(); // "Driver" or "Owner"

        // Check if a user with this role exists
        var userExists = await _dbContext
            .Users.Include(u => u.Role)
            .AnyAsync(u => u.Role.Name == roleName && !u.IsDeleted);

        User user;

        // If no user exists with the role, create one
        if (!userExists)
        {
            // First check if the role exists
            var role = await _dbContext.UserRoles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                // Create the role if it doesn't exist
                role = new UserRole { Name = roleName };
                await _dbContext.UserRoles.AddAsync(role);
                await _dbContext.SaveChangesAsync();
            }

            // Create encryption key for the user
            var (key, iv) = await keyManagementService.GenerateKeyAsync();
            string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);
            string encryptedPhoneNumber = await aesEncryptionService.Encrypt("1234567890", key, iv);

            EncryptionKey newEncryptionKey = new() { EncryptedKey = encryptedKey, IV = iv };
            await context.EncryptionKeys.AddAsync(newEncryptionKey);

            // Create a new user with this role
            user = new User
            {
                Email = $"test.{roleName.ToLower()}@example.com",
                Name = $"Test {roleName}",
                RoleId = role.Id,
                Password = "TestPassword123",
                EncryptionKeyId = newEncryptionKey.Id,
                Address = "Test Address",
                DateOfBirth = DateTimeOffset.UtcNow.AddYears(-25), // 25 years old
                Phone = encryptedPhoneNumber,
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            // Get an existing user with this role
            user = await _dbContext
                .Users.Include(u => u.Role)
                .FirstAsync(u => u.Role.Name == roleName && !u.IsDeleted);
        }

        var feedback = new Feedback
        {
            UserId = user.Id,
            BookingId = bookingId,
            Type = type,
            Content = "Test feedback",
            Point = rating,
        };

        await _dbContext.Feedbacks.AddAsync(feedback);
        await _dbContext.SaveChangesAsync();

        return feedback;
    }

    private async Task<InspectionStatus> CreateInspectionStatus(string name)
    {
        var status = new InspectionStatus { Name = name };

        await _dbContext.InspectionStatuses.AddAsync(status);
        await _dbContext.SaveChangesAsync();

        return status;
    }

    private async Task<InspectionSchedule> CreateInspectionSchedule(
        Guid carId,
        Guid technicianId,
        Guid statusId,
        Guid createdById
    )
    {
        var schedule = new InspectionSchedule
        {
            CarId = carId,
            TechnicianId = technicianId,
            InspectionStatusId = statusId,
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
            InspectionAddress = "Test Address",
            Note = "Test note",
            CreatedBy = createdById,
        };

        await _dbContext.InspectionSchedules.AddAsync(schedule);
        await _dbContext.SaveChangesAsync();

        return schedule;
    }
}
