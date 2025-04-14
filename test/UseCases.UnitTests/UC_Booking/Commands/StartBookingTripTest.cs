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
    private const string TEST_SIGNATURE_BASE64 =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
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
        var command = new StartBookingTrip.Command(
            Guid.NewGuid(),
            _latitude,
            _longitude,
            TEST_SIGNATURE_BASE64
        );

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
        var command = new StartBookingTrip.Command(
            Guid.NewGuid(),
            _latitude,
            _longitude,
            TEST_SIGNATURE_BASE64
        );

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
        var command = new StartBookingTrip.Command(
            booking.Id,
            _latitude,
            _longitude,
            TEST_SIGNATURE_BASE64
        );

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

        // Setup contract for the booking
        var contract = new Contract
        {
            BookingId = booking.Id,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Terms = "Sample contract terms",
            Status = ContractStatusEnum.Pending,
        };
        await _dbContext.Contracts.AddAsync(contract);
        await _dbContext.SaveChangesAsync();

        var handler = new StartBookingTrip.Handler(
            _dbContext,
            _geometryFactory,
            _logger,
            _currentUser
        );
        var command = new StartBookingTrip.Command(
            booking.Id,
            _latitude,
            _longitude,
            TEST_SIGNATURE_BASE64
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Đã bắt đầu chuyến đi", result.SuccessMessage);

        var updatedBooking = await _dbContext.Bookings.FirstAsync(b => b.Id == booking.Id);

        Assert.Equal(BookingStatusEnum.Ongoing, updatedBooking.Status);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesContractWithDriverSignature()
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

        // Add GPS to the car
        var gps = new GPSDevice
        {
            Id = Guid.NewGuid(),
            Name = "Test GPS Device",
            OSBuildId = string.Concat("GPS", Guid.NewGuid().ToString().AsSpan(0, 8)),
            Status = DeviceStatusEnum.Available,
        };
        await _dbContext.GPSDevices.AddAsync(gps);
        await _dbContext.SaveChangesAsync();

        var location = _geometryFactory.CreatePoint(
            new Coordinate((double)_longitude, (double)_latitude)
        );
        location.SRID = 4326;

        var carGPS = new CarGPS
        {
            CarId = car.Id,
            DeviceId = gps.Id,
            Location = location,
        };
        await _dbContext.CarGPSes.AddAsync(carGPS);
        await _dbContext.SaveChangesAsync();

        var booking = await TestDataCreateBooking.CreateTestBooking(
            _dbContext,
            driver.Id,
            car.Id,
            BookingStatusEnum.ReadyForPickup
        );

        // Setup contract for the booking
        var contract = new Contract
        {
            BookingId = booking.Id,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Terms = "Sample contract terms",
            Status = ContractStatusEnum.Pending,
        };
        await _dbContext.Contracts.AddAsync(contract);
        await _dbContext.SaveChangesAsync();

        var handler = new StartBookingTrip.Handler(
            _dbContext,
            _geometryFactory,
            _logger,
            _currentUser
        );
        var command = new StartBookingTrip.Command(
            booking.Id,
            _latitude,
            _longitude,
            TEST_SIGNATURE_BASE64
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Đã bắt đầu chuyến đi", result.SuccessMessage);

        // Verify booking status is updated
        var updatedBooking = await _dbContext.Bookings.FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatusEnum.Ongoing, updatedBooking.Status);

        // Verify contract signature is updated
        var updatedContract = await _dbContext.Contracts.FirstAsync(c => c.BookingId == booking.Id);
        Assert.Equal(TEST_SIGNATURE_BASE64, updatedContract.DriverSignature);
        Assert.NotNull(updatedContract.DriverSignatureDate);

        // Verify tracking is created
        var tracking = await _dbContext.TripTrackings.FirstAsync(t => t.BookingId == booking.Id);
        Assert.NotNull(tracking);
        Assert.Equal(0, tracking.Distance);
        Assert.Equal(0, tracking.CumulativeDistance);
    }

    [Fact]
    public void Handle_EmptySignature_ValidationFails()
    {
        // Arrange
        var validator = new StartBookingTrip.Validator();
        var command = new StartBookingTrip.Command(
            Guid.NewGuid(),
            _latitude,
            _longitude,
            string.Empty
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "Chữ ký không được để trống");
    }

    [Fact]
    public void Handle_InvalidLatitude_ValidationFails()
    {
        // Arrange
        var validator = new StartBookingTrip.Validator();
        var command = new StartBookingTrip.Command(
            Guid.NewGuid(),
            100.0m, // Invalid latitude (outside -90 to 90 range)
            _longitude,
            TEST_SIGNATURE_BASE64
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage == "Cần đến gần chiếc xe thì mới bắt đầu được"
        );
    }

    [Fact]
    public void Handle_InvalidLongitude_ValidationFails()
    {
        // Arrange
        var validator = new StartBookingTrip.Validator();
        var command = new StartBookingTrip.Command(
            Guid.NewGuid(),
            _latitude,
            200.0m, // Invalid longitude (outside -180 to 180 range)
            TEST_SIGNATURE_BASE64
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage == "Cần đến gần chiếc xe thì mới bắt đầu được"
        );
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
