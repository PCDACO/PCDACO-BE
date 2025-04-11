using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_Booking.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Booking.Commands;

[Collection("Test Collection")]
public class StartBookingTripTests(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly GeometryFactory _geometryFactory = new();
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly ILogger<StartBookingTrip.Handler> _logger = LoggerFactory
        .Create(builder => builder.AddConsole())
        .CreateLogger<StartBookingTrip.Handler>();
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    private readonly decimal _latitude = 10.7756587m;
    private readonly decimal _longitude = 106.7004238m;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotDriver_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(testUser);

        var handler = new StartBookingTrip.Handler(
            _dbContext,
            _geometryFactory,
            _logger,
            _currentUser
        );
        var command = new StartBookingTrip.Command(Guid.NewGuid(), _latitude, _longitude);

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

        var handler = new StartBookingTrip.Handler(
            _dbContext,
            _geometryFactory,
            _logger,
            _currentUser
        );
        var command = new StartBookingTrip.Command(Guid.NewGuid(), _latitude, _longitude);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy booking", result.Errors);
    }

    [Theory]
    [InlineData(BookingStatusEnum.Pending)]
    [InlineData(BookingStatusEnum.Rejected)]
    [InlineData(BookingStatusEnum.Ongoing)]
    [InlineData(BookingStatusEnum.Completed)]
    [InlineData(BookingStatusEnum.Cancelled)]
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

        // Setup car and booking
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Available
        );

        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            status
        );

        var handler = new StartBookingTrip.Handler(
            _dbContext,
            _geometryFactory,
            _logger,
            _currentUser
        );
        var command = new StartBookingTrip.Command(booking.Id, _latitude, _longitude);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains($"Không thể phê duyệt booking ở trạng thái {status}", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesBookingStatusToOngoing()
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

        // Setup car and booking
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var car = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            model.Id,
            transmissionType,
            fuelType,
            CarStatusEnum.Available
        );

        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            BookingStatusEnum.ReadyForPickup
        );

        var handler = new StartBookingTrip.Handler(
            _dbContext,
            _geometryFactory,
            _logger,
            _currentUser
        );
        var command = new StartBookingTrip.Command(booking.Id, _latitude, _longitude);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Đã bắt đầu chuyến đi", result.SuccessMessage);

        var updatedBooking = await _dbContext.Bookings.FirstAsync(b => b.Id == booking.Id);

        Assert.Equal(BookingStatusEnum.Ongoing, updatedBooking.Status);
    }

    // [Fact]
    // public async Task Handle_OngoingStatusNotFound_ReturnsNotFound()
    // {
    //     // Arrange
    //     UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
    //     var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
    //     _currentUser.SetUser(driver);
    //
    //     await _dbContext.SaveChangesAsync();
    //
    //     var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
    //     var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
    //     var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
    //         _dbContext,
    //         "Automatic"
    //     );
    //     var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
    //     var owner = await TestDataCreateUser.CreateTestUser(
    //         _dbContext,
    //         await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner")
    //     );
    //
    //     var car = await TestDataCreateCar.CreateTestCar(
    //         _dbContext,
    //         owner.Id,
    //         model.Id,
    //         transmissionType,
    //         fuelType,
    //         CarStatusEnum.Available
    //     );
    //
    //     var booking = await TestDataCreateBooking.CreateTestBooking(
    //         _dbContext,
    //         driver.Id,
    //         car.Id,
    //         BookingStatusEnum.Ongoing
    //     );
    //
    //     var handler = new StartBookingTrip.Handler(_dbContext, _geometryFactory, _logger, _currentUser);
    //     var command = new StartBookingTrip.Command(booking.Id, _latitude, _longitude);
    //
    //     // Act
    //     var result = await handler.Handle(command, CancellationToken.None);
    //
    //     // Assert
    //     Assert.Equal(ResultStatus.NotFound, result.Status);
    //     Assert.Contains("Không tìm thấy trạng thái phù hợp", result.Errors);
    // }
}
