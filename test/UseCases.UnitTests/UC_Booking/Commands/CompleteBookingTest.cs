using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.Services.PayOSService;
using UseCases.UC_Booking.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.Mocks;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Booking.Commands;

[Collection("Test Collection")]
public class CompleteBookingTests(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly IPaymentService _paymentService = new TestDataPaymentService();
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotDriver_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(testUser);

        var handler = new CompleteBooking.Handler(_dbContext, _currentUser, _paymentService);
        var command = new CompleteBooking.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này !", result.Errors);
    }

    [Fact]
    public async Task Handle_BookingNotFound_ReturnsNotFound()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new CompleteBooking.Handler(_dbContext, _currentUser, _paymentService);
        var command = new CompleteBooking.Command(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy booking", result.Errors);
    }

    [Theory]
    [InlineData(BookingStatusEnum.Pending)]
    [InlineData(BookingStatusEnum.Approved)]
    [InlineData(BookingStatusEnum.Rejected)]
    [InlineData(BookingStatusEnum.Completed)]
    [InlineData(BookingStatusEnum.Cancelled)]
    public async Task Handle_InvalidBookingStatus_ReturnsConflict(BookingStatusEnum status)
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var bookingStatuses = await TestDataBookingStatus.CreateTestBookingStatuses(_dbContext);

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(driver);

        // Setup car and booking
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        var carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            carStatus
        );

        var statusId = bookingStatuses.First(s => s.Name == status.ToString()).Id;
        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            statusId
        );

        var handler = new CompleteBooking.Handler(_dbContext, _currentUser, _paymentService);
        var command = new CompleteBooking.Command(booking.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains($"Không thể phê duyệt booking ở trạng thái {status}", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesBookingStatusToCompleted()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var bookingStatuses = await TestDataBookingStatus.CreateTestBookingStatuses(_dbContext);

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com"
        );
        _currentUser.SetUser(driver);

        // Setup car and booking
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        var carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            carStatus
        );

        var ongoingStatusId = bookingStatuses
            .First(s => s.Name == BookingStatusEnum.Ongoing.ToString())
            .Id;
        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            ongoingStatusId
        );

        var handler = new CompleteBooking.Handler(_dbContext, _currentUser, _paymentService);
        var command = new CompleteBooking.Command(booking.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Đã hoàn thành chuyến đi", result.Value.Message);
        Assert.Equal("http://mock-checkout-url", result.Value.PaymentUrl);
        Assert.Equal("mock-qr-code", result.Value.QrCode);

        var updatedBooking = await _dbContext
            .Bookings.Include(b => b.Status)
            .FirstAsync(b => b.Id == booking.Id);

        Assert.Equal(BookingStatusEnum.Completed.ToString(), updatedBooking.Status.Name);
        Assert.True(updatedBooking.ActualReturnTime > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task Handle_StatusNotFound_ReturnsNotFound()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(driver);

        // Create booking with ongoing status but remove completed status
        var bookingStatuses = await TestDataBookingStatus.CreateTestBookingStatuses(_dbContext);
        var completedStatus = bookingStatuses.First(s =>
            s.Name == BookingStatusEnum.Completed.ToString()
        );
        _dbContext.BookingStatuses.Remove(completedStatus);
        await _dbContext.SaveChangesAsync();

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        var carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");
        var owner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
        );

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            carStatus
        );

        var ongoingStatusId = bookingStatuses
            .First(s => s.Name == BookingStatusEnum.Ongoing.ToString())
            .Id;
        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            ongoingStatusId
        );

        var handler = new CompleteBooking.Handler(_dbContext, _currentUser, _paymentService);
        var command = new CompleteBooking.Command(booking.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy trạng thái phù hợp", result.Errors);
    }
}
