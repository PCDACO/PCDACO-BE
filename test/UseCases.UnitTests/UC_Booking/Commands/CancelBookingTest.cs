using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.Services.EmailService;
using UseCases.UC_Booking.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Booking.Commands;

[Collection("Test Collection")]
public class CancelBookingTests(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly Mock<IEmailService> _emailServiceMock = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotDriverOrOwner_ReturnsError()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(admin);

        var car = await CreateTestCar(_dbContext, owner.Id);
        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            BookingStatusEnum.Pending
        );

        var handler = new CancelBooking.Handler(_dbContext, _currentUser, _emailServiceMock.Object);
        var command = new CancelBooking.Command(booking.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(
            "Bạn không có quyền thực hiện chức năng này với booking này!",
            result.Errors
        );
    }

    [Fact]
    public async Task Handle_BookingNotFound_ReturnsNotFound()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new CancelBooking.Handler(_dbContext, _currentUser, _emailServiceMock.Object);
        var command = new CancelBooking.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy booking", result.Errors);
    }

    [Theory]
    [InlineData(BookingStatusEnum.Rejected)]
    [InlineData(BookingStatusEnum.Ongoing)]
    [InlineData(BookingStatusEnum.Completed)]
    [InlineData(BookingStatusEnum.Cancelled)]
    [InlineData(BookingStatusEnum.Expired)]
    public async Task Handle_InvalidBookingStatus_ReturnsConflict(BookingStatusEnum status)
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(driver);

        var car = await CreateTestCar(_dbContext, owner.Id);
        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            status
        );

        var handler = new CancelBooking.Handler(_dbContext, _currentUser, _emailServiceMock.Object);
        var command = new CancelBooking.Command(booking.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains($"Không thể hủy booking ở trạng thái {status}", result.Errors);
    }

    [Theory]
    [InlineData(BookingStatusEnum.Pending)]
    [InlineData(BookingStatusEnum.Approved)]
    public async Task Handle_DriverCancellation_UpdatesBookingStatusAndSendsEmails(
        BookingStatusEnum initialStatus
    )
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(driver);

        var car = await CreateTestCar(_dbContext, owner.Id);
        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            initialStatus
        );

        var handler = new CancelBooking.Handler(_dbContext, _currentUser, _emailServiceMock.Object);
        var command = new CancelBooking.Command(booking.Id, "Test cancellation reason");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Đã hủy booking thành công", result.SuccessMessage);

        var updatedBooking = await _dbContext.Bookings.FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatusEnum.Cancelled, updatedBooking.Status);
        Assert.Equal("Test cancellation reason", updatedBooking.Note);

        // Verify email was sent
        _emailServiceMock.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(2)
        );
    }

    [Theory]
    [InlineData(BookingStatusEnum.Pending)]
    [InlineData(BookingStatusEnum.Approved)]
    public async Task Handle_OwnerCancellation_UpdatesBookingStatusAndAppliesPenalty(
        BookingStatusEnum initialStatus
    )
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(owner);

        // Create Refund transaction type
        var refundType = await TestDataTransactionType.CreateTestTransactionType(
            _dbContext,
            "Refund"
        );

        var car = await CreateTestCar(_dbContext, owner.Id);
        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            initialStatus
        );

        // Set initial balance for owner and total amount for booking
        owner.Balance = 1000000; // 1 million VND
        booking.TotalAmount = 500000; // 500k VND
        booking.IsPaid = true;
        await _dbContext.SaveChangesAsync();

        var handler = new CancelBooking.Handler(_dbContext, _currentUser, _emailServiceMock.Object);
        var command = new CancelBooking.Command(booking.Id, "Test cancellation reason");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Đã hủy booking thành công", result.SuccessMessage);

        var updatedBooking = await _dbContext.Bookings.FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatusEnum.Cancelled, updatedBooking.Status);
        Assert.Equal("Test cancellation reason", updatedBooking.Note);

        // Verify email was sent
        _emailServiceMock.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task Handle_DriverCancellation_ExceedsLimit_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(driver);

        var car = await CreateTestCar(_dbContext, owner.Id);

        // Create 5 cancelled bookings
        for (int i = 0; i < 5; i++)
        {
            var booking = await TestDataCreateBooking.CreateTestBooking(
                _dbContext,
                driver.Id,
                car.Id,
                BookingStatusEnum.Cancelled
            );
            booking.UpdatedAt = DateTimeOffset.UtcNow.AddDays(-15); // Within 30 days
            await _dbContext.SaveChangesAsync();
        }

        // Create a new booking to cancel
        var newBooking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            BookingStatusEnum.Pending
        );

        var handler = new CancelBooking.Handler(_dbContext, _currentUser, _emailServiceMock.Object);
        var command = new CancelBooking.Command(newBooking.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Bạn đã hủy quá số lần cho phép trong 30 ngày", result.Errors);
    }

    private static async Task<Car> CreateTestCar(AppDBContext context, Guid ownerId)
    {
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(context);
        var model = await TestDataCreateModel.CreateTestModel(context, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            context,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(context, "Electric");

        return await TestDataCreateCar.CreateTestCar(
            context,
            ownerId,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Available
        );
    }
}
