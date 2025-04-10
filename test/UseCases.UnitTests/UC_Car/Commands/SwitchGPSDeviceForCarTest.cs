using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Persistance.Data;
using UseCases.UC_GPSDevice.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_GPSDevice.Commands;

[Collection("Test Collection")]
public class SwitchGPSDeviceForCarTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ValidRequest_NewDeviceAssociation_Succeeds()
    {
        // Arrange
        var (car, _) = await SetupTestCar();
        var device = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext);

        var handler = new SwitchGPSDeviceForCar.Handler(_dbContext, _geometryFactory);
        var command = new SwitchGPSDeviceForCar.Command(
            CarId: car.Id,
            GPSDeviceId: device.Id,
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Cập nhật thiết bị GPS cho xe thành công", result.SuccessMessage);

        // Verify CarGPS was created
        var carGPS = await _dbContext.CarGPSes.FirstOrDefaultAsync(c =>
            c.CarId == car.Id && c.DeviceId == device.Id
        );
        Assert.NotNull(carGPS);
        Assert.Equal(10.7756587, carGPS.Location.Y, 6); // Y is latitude
        Assert.Equal(106.7004238, carGPS.Location.X, 6); // X is longitude
        Assert.False(carGPS.IsDeleted);

        // Verify GPS device status was updated
        var updatedDevice = await _dbContext.GPSDevices.FirstOrDefaultAsync(d => d.Id == device.Id);
        Assert.NotNull(updatedDevice);
        Assert.Equal(DeviceStatusEnum.InUsed, updatedDevice.Status);
    }

    [Fact]
    public async Task Handle_SwitchingDeviceFromOtherCar_ChangesOldCarStatusToPending()
    {
        // Arrange
        var (firstCar, _) = await SetupTestCar();
        var (secondCar, _) = await SetupTestCar("Second Car");
        var device = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext);

        // Create initial association with first car
        var initialLocation = _geometryFactory.CreatePoint(new Coordinate(106.6, 10.6));
        initialLocation.SRID = 4326;

        var existingCarGPS = new CarGPS
        {
            CarId = firstCar.Id,
            DeviceId = device.Id,
            Location = initialLocation,
            IsDeleted = false,
        };
        await _dbContext.CarGPSes.AddAsync(existingCarGPS);
        await _dbContext.SaveChangesAsync();

        // Now switch the device to the second car
        var handler = new SwitchGPSDeviceForCar.Handler(_dbContext, _geometryFactory);
        var command = new SwitchGPSDeviceForCar.Command(
            CarId: secondCar.Id,
            GPSDeviceId: device.Id,
            Longtitude: 107.0,
            Latitude: 11.0
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify GPS association was updated to second car
        var updatedCarGPS = await _dbContext.CarGPSes.FirstOrDefaultAsync(c =>
            c.DeviceId == device.Id
        );
        Assert.NotNull(updatedCarGPS);
        Assert.Equal(secondCar.Id, updatedCarGPS.CarId);
        Assert.Equal(11.0, updatedCarGPS.Location.Y, 6);
        Assert.Equal(107.0, updatedCarGPS.Location.X, 6);
    }

    [Fact]
    public async Task Handle_CarNotFound_ReturnsError()
    {
        // Arrange
        var nonExistentCarId = Guid.NewGuid();
        var device = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext);

        var handler = new SwitchGPSDeviceForCar.Handler(_dbContext, _geometryFactory);
        var command = new SwitchGPSDeviceForCar.Command(
            CarId: nonExistentCarId,
            GPSDeviceId: device.Id,
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarNotFound, result.Errors);

        // Verify no CarGPS was created
        var carGPSCount = await _dbContext.CarGPSes.CountAsync();
        Assert.Equal(0, carGPSCount);
    }

    [Fact]
    public async Task Handle_GPSDeviceNotFound_ReturnsError()
    {
        // Arrange
        var (car, _) = await SetupTestCar();
        var nonExistentDeviceId = Guid.NewGuid();

        var handler = new SwitchGPSDeviceForCar.Handler(_dbContext, _geometryFactory);
        var command = new SwitchGPSDeviceForCar.Command(
            CarId: car.Id,
            GPSDeviceId: nonExistentDeviceId,
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.GPSDeviceNotFound, result.Errors);

        // Verify no CarGPS was created
        var carGPSCount = await _dbContext.CarGPSes.CountAsync();
        Assert.Equal(0, carGPSCount);
    }

    [Fact]
    public async Task Handle_RequestedCarAlreadyHasCarGPS_ReturnError()
    {
        // Arrange
        var (car, _) = await SetupTestCar();
        var device = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext);

        // Create deleted association with car
        var initialLocation = _geometryFactory.CreatePoint(new Coordinate(106.6, 10.6));
        initialLocation.SRID = 4326;

        var deletedCarGPS = new CarGPS
        {
            CarId = car.Id,
            DeviceId = device.Id,
            Location = initialLocation,
            IsDeleted = true, // This is deleted
        };
        await _dbContext.CarGPSes.AddAsync(deletedCarGPS);
        await _dbContext.SaveChangesAsync();

        // Now create a new association
        var handler = new SwitchGPSDeviceForCar.Handler(_dbContext, _geometryFactory);
        var command = new SwitchGPSDeviceForCar.Command(
            CarId: car.Id,
            GPSDeviceId: device.Id,
            Longtitude: 107.0,
            Latitude: 11.0
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(
            "Không thể đổi thiết bị GPS cho xe này vì xe đã có thiết bị GPS",
            result.Errors
        );
    }

    [Theory]
    [InlineData(BookingStatusEnum.Pending)]
    [InlineData(BookingStatusEnum.Ongoing)]
    [InlineData(BookingStatusEnum.ReadyForPickup)]
    [InlineData(BookingStatusEnum.Approved)]
    public async Task Handle_DeviceUsedWithActiveBooking_ReturnsError(
        BookingStatusEnum bookingStatus
    )
    {
        // Arrange
        var (firstCar, owner) = await SetupTestCar();
        var (secondCar, _) = await SetupTestCar("Second Car");
        var device = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext);

        // Create active GPS association for first car
        var location = _geometryFactory.CreatePoint(new Coordinate(106.6, 10.6));
        location.SRID = 4326;

        var existingCarGPS = new CarGPS
        {
            CarId = firstCar.Id,
            DeviceId = device.Id,
            Location = location,
            IsDeleted = false,
        };
        await _dbContext.CarGPSes.AddAsync(existingCarGPS);

        // Create active booking for the first car
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@example.com"
        );

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CarId = firstCar.Id,
            UserId = driver.Id,
            Status = bookingStatus,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(1),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(1),
            BasePrice = 100,
            PlatformFee = 20,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 120,
            Note = "Test booking",
            IsPaid = true,
        };
        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Try to switch device to second car
        var handler = new SwitchGPSDeviceForCar.Handler(_dbContext, _geometryFactory);
        var command = new SwitchGPSDeviceForCar.Command(
            CarId: secondCar.Id,
            GPSDeviceId: device.Id,
            Longtitude: 107.0,
            Latitude: 11.0
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(
            "Không thể đổi thiết bị GPS khi gps device đang được dùng cho xe có đơn đặt xe",
            result.Errors
        );

        // Verify GPS association is unchanged
        var unchangedGPS = await _dbContext.CarGPSes.FirstOrDefaultAsync(c =>
            c.DeviceId == device.Id
        );
        Assert.NotNull(unchangedGPS);
        Assert.Equal(firstCar.Id, unchangedGPS.CarId);
        Assert.Equal(106.6, unchangedGPS.Location.X, 6); // Original X coordinate
        Assert.Equal(10.6, unchangedGPS.Location.Y, 6); // Original Y coordinate
    }

    [Theory]
    [InlineData(BookingStatusEnum.Completed)]
    [InlineData(BookingStatusEnum.Cancelled)]
    [InlineData(BookingStatusEnum.Rejected)]
    [InlineData(BookingStatusEnum.Expired)]
    public async Task Handle_DeviceWithInactiveBooking_Succeeds(BookingStatusEnum bookingStatus)
    {
        // Arrange
        var (firstCar, owner) = await SetupTestCar();
        var (secondCar, _) = await SetupTestCar("Second Car");
        var device = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext);

        // Create active GPS association for first car
        var location = _geometryFactory.CreatePoint(new Coordinate(106.6, 10.6));
        location.SRID = 4326;

        var existingCarGPS = new CarGPS
        {
            CarId = firstCar.Id,
            DeviceId = device.Id,
            Location = location,
            IsDeleted = false,
        };
        await _dbContext.CarGPSes.AddAsync(existingCarGPS);

        // Create inactive booking for the first car
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@example.com"
        );

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CarId = firstCar.Id,
            UserId = driver.Id,
            Status = bookingStatus, // Inactive status
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(1),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(1),
            BasePrice = 100,
            PlatformFee = 20,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 120,
            Note = "Test booking",
            IsPaid = true,
        };
        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Try to switch device to second car
        var handler = new SwitchGPSDeviceForCar.Handler(_dbContext, _geometryFactory);
        var command = new SwitchGPSDeviceForCar.Command(
            CarId: secondCar.Id,
            GPSDeviceId: device.Id,
            Longtitude: 107.0,
            Latitude: 11.0
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Cập nhật thiết bị GPS cho xe thành công", result.SuccessMessage);

        // Verify GPS association was updated to second car
        var updatedCarGPS = await _dbContext.CarGPSes.FirstOrDefaultAsync(c =>
            c.DeviceId == device.Id
        );
        Assert.NotNull(updatedCarGPS);
        Assert.Equal(secondCar.Id, updatedCarGPS.CarId);
        Assert.Equal(107.0, updatedCarGPS.Location.X, 6);
        Assert.Equal(11.0, updatedCarGPS.Location.Y, 6);
    }

    [Fact]
    public async Task Handle_MultipleBookings_OnlyActiveBlocksSwitch()
    {
        // Arrange
        var (firstCar, owner) = await SetupTestCar();
        var (secondCar, _) = await SetupTestCar("Second Car");
        var device = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext);

        // Create active GPS association for first car
        var location = _geometryFactory.CreatePoint(new Coordinate(106.6, 10.6));
        location.SRID = 4326;

        var existingCarGPS = new CarGPS
        {
            CarId = firstCar.Id,
            DeviceId = device.Id,
            Location = location,
            IsDeleted = false,
        };
        await _dbContext.CarGPSes.AddAsync(existingCarGPS);

        // Create driver user
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@example.com"
        );

        // Create both active and inactive bookings for the car
        var activeBooking = new Booking
        {
            Id = Guid.NewGuid(),
            CarId = firstCar.Id,
            UserId = driver.Id,
            Status = BookingStatusEnum.Ongoing, // Active status
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(1),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(1),
            BasePrice = 100,
            PlatformFee = 20,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 120,
            Note = "Active booking",
            IsPaid = true,
        };

        var inactiveBooking = new Booking
        {
            Id = Guid.NewGuid(),
            CarId = firstCar.Id,
            UserId = driver.Id,
            Status = BookingStatusEnum.Completed, // Inactive status
            StartTime = DateTimeOffset.UtcNow.AddDays(-10),
            EndTime = DateTimeOffset.UtcNow.AddDays(-9),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(-9),
            BasePrice = 100,
            PlatformFee = 20,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 120,
            Note = "Inactive booking",
            IsPaid = true,
        };

        await _dbContext.Bookings.AddRangeAsync(activeBooking, inactiveBooking);
        await _dbContext.SaveChangesAsync();

        // Try to switch device to second car
        var handler = new SwitchGPSDeviceForCar.Handler(_dbContext, _geometryFactory);
        var command = new SwitchGPSDeviceForCar.Command(
            CarId: secondCar.Id,
            GPSDeviceId: device.Id,
            Longtitude: 107.0,
            Latitude: 11.0
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(
            "Không thể đổi thiết bị GPS khi gps device đang được dùng cho xe có đơn đặt xe",
            result.Errors
        );
    }

    [Fact]
    public void Validator_EmptyCarId_FailsValidation()
    {
        // Arrange
        var validator = new SwitchGPSDeviceForCar.Validator();
        var command = new SwitchGPSDeviceForCar.Command(
            CarId: Guid.Empty,
            GPSDeviceId: Guid.NewGuid(),
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.CarId)
            .WithErrorMessage("ID xe không được để trống");
    }

    [Fact]
    public void Validator_EmptyGPSDeviceId_FailsValidation()
    {
        // Arrange
        var validator = new SwitchGPSDeviceForCar.Validator();
        var command = new SwitchGPSDeviceForCar.Command(
            CarId: Guid.NewGuid(),
            GPSDeviceId: Guid.Empty,
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.GPSDeviceId)
            .WithErrorMessage("ID thiết bị GPS không được để trống");
    }

    #region Helper Methods

    private async Task<(Car car, User owner)> SetupTestCar(string carName = "Test Car")
    {
        // Create owner role
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        // Create owner
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);

        // Create manufacturer and model
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        // Create transmission and fuel types
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create pickup location
        var pickupLocation = _geometryFactory.CreatePoint(new Coordinate(106.7004238, 10.7756587));
        pickupLocation.SRID = 4326;

        // Create car
        var car = new Car
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OwnerId = owner.Id,
            ModelId = model.Id,
            LicensePlate = "TEST-12345",
            FuelTypeId = fuelType.Id,
            TransmissionTypeId = transmissionType.Id,
            Status = CarStatusEnum.Available,
            Color = "Red",
            Seat = 4,
            Description = carName,
            FuelConsumption = 7.5m,
            Price = 100m,
            RequiresCollateral = false,
            Terms = "Standard terms",
            PickupLocation = pickupLocation,
            PickupAddress = "Test Address",
            IsDeleted = false,
        };

        await _dbContext.Cars.AddAsync(car);

        // Create car statistics
        var carStatistic = new CarStatistic
        {
            CarId = car.Id,
            TotalBooking = 0,
            AverageRating = 0,
        };

        await _dbContext.CarStatistics.AddAsync(carStatistic);
        await _dbContext.SaveChangesAsync();

        return (car, owner);
    }

    #endregion
}
