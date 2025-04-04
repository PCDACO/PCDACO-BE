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

namespace UseCases.UnitTests.UC_Car.Commands;

[Collection("Test Collection")]
public class AssignDeviceToCarTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ValidRequest_AddsGpsDeviceSuccessfully()
    {
        // Arrange
        var (car, _) = await SetupTestCar();

        string osBuildId = "TESTBUILD123";
        string deviceName = "Test GPS Device";
        double longitude = 106.7004238;
        double latitude = 10.7756587;

        var handler = new AssignDeviceToCar.Handler(_dbContext, _geometryFactory);
        var command = new AssignDeviceToCar.Command(
            CarId: car.Id,
            OSBuildId: osBuildId,
            DeviceName: deviceName,
            Longtitude: longitude,
            Latitude: latitude
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains(ResponseMessages.Created, result.SuccessMessage);

        // Verify device was created
        var device = await _dbContext.GPSDevices.FirstOrDefaultAsync(d => d.OSBuildId == osBuildId);
        Assert.NotNull(device);
        Assert.Equal(deviceName, device.Name);
        Assert.Equal(DeviceStatusEnum.InUsed, device.Status);

        // Verify CarGPS was created
        var carGPS = await _dbContext.CarGPSes.FirstOrDefaultAsync(c =>
            c.CarId == car.Id && c.DeviceId == device.Id
        );
        Assert.NotNull(carGPS);
        Assert.Equal(latitude, carGPS.Location.Y, 6); // Y is latitude
        Assert.Equal(longitude, carGPS.Location.X, 6); // X is longitude
        Assert.False(carGPS.IsDeleted);
    }

    [Fact]
    public async Task Handle_CarNotFound_ReturnsError()
    {
        // Arrange
        var nonExistentCarId = Guid.NewGuid();
        string osBuildId = "TESTBUILD456";
        string deviceName = "Test GPS Device";

        var handler = new AssignDeviceToCar.Handler(_dbContext, _geometryFactory);
        var command = new AssignDeviceToCar.Command(
            CarId: nonExistentCarId,
            OSBuildId: osBuildId,
            DeviceName: deviceName,
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarNotFound, result.Errors);

        // Verify no device was created
        var deviceCount = await _dbContext.GPSDevices.CountAsync();
        Assert.Equal(0, deviceCount);

        // Verify no CarGPS was created
        var carGPSCount = await _dbContext.CarGPSes.CountAsync();
        Assert.Equal(0, carGPSCount);
    }

    [Fact]
    public async Task Handle_ExistingDevice_ReusesDevice()
    {
        // Arrange
        var (car, _) = await SetupTestCar();

        // Create an existing device
        string existingOsBuildId = "EXISTING-BUILD";
        var existingDevice = new GPSDevice
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OSBuildId = existingOsBuildId,
            Name = "Existing Device",
            Status = DeviceStatusEnum.Available,
            IsDeleted = false,
        };

        await _dbContext.GPSDevices.AddAsync(existingDevice);
        await _dbContext.SaveChangesAsync();

        var handler = new AssignDeviceToCar.Handler(_dbContext, _geometryFactory);
        var command = new AssignDeviceToCar.Command(
            CarId: car.Id,
            OSBuildId: existingOsBuildId,
            DeviceName: "Updated Device Name", // Different name
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify the device count hasn't changed
        var deviceCount = await _dbContext.GPSDevices.CountAsync();
        Assert.Equal(1, deviceCount);

        // Verify a new CarGPS was created using the existing device
        var carGPS = await _dbContext.CarGPSes.FirstOrDefaultAsync(c =>
            c.CarId == car.Id && c.DeviceId == existingDevice.Id
        );
        Assert.NotNull(carGPS);
    }

    [Fact]
    public async Task Handle_ExistingCarGps_ReturnsError()
    {
        // Arrange
        var (car, _) = await SetupTestCar();

        // Create a GPS device
        var device = new GPSDevice
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OSBuildId = "DEVICE-123",
            Name = "Test Device",
            Status = DeviceStatusEnum.Available,
            IsDeleted = false,
        };

        await _dbContext.GPSDevices.AddAsync(device);

        // Create an existing CarGPS
        var location = _geometryFactory.CreatePoint(new Coordinate(106.6, 10.6));
        location.SRID = 4326;

        var existingCarGPS = new CarGPS
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            DeviceId = device.Id,
            Location = location,
            IsDeleted = false,
        };

        await _dbContext.CarGPSes.AddAsync(existingCarGPS);
        await _dbContext.SaveChangesAsync();

        var handler = new AssignDeviceToCar.Handler(_dbContext, _geometryFactory);
        var command = new AssignDeviceToCar.Command(
            CarId: car.Id,
            OSBuildId: "DEVICE-123",
            DeviceName: "Test Device",
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarGPSIsExisted, result.Errors);

        // Verify the CarGPS data hasn't changed
        var unchangedCarGPS = await _dbContext.CarGPSes.FirstOrDefaultAsync(c =>
            c.CarId == car.Id && c.DeviceId == device.Id
        );
        Assert.Equal(10.6, unchangedCarGPS!.Location.Y, 6);
        Assert.Equal(106.6, unchangedCarGPS.Location.X, 6);
    }

    [Fact]
    public async Task Handle_RestoresDeletedCarGps_WhenFound()
    {
        // Arrange
        var (car, _) = await SetupTestCar();

        // Create a GPS device
        var device = new GPSDevice
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OSBuildId = "DEVICE-456",
            Name = "Test Device",
            Status = DeviceStatusEnum.Available,
            IsDeleted = false,
        };

        await _dbContext.GPSDevices.AddAsync(device);

        // Create a deleted CarGPS
        var location = _geometryFactory.CreatePoint(new Coordinate(106.6, 10.6));
        location.SRID = 4326;

        var deletedCarGPS = new CarGPS
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            DeviceId = device.Id,
            Location = location,
            IsDeleted = true,
        };

        await _dbContext.CarGPSes.AddAsync(deletedCarGPS);
        await _dbContext.SaveChangesAsync();

        // New coordinates
        double newLongitude = 106.7004238;
        double newLatitude = 10.7756587;

        var handler = new AssignDeviceToCar.Handler(_dbContext, _geometryFactory);
        var command = new AssignDeviceToCar.Command(
            CarId: car.Id,
            OSBuildId: "DEVICE-456",
            DeviceName: "Test Device",
            Longtitude: newLongitude,
            Latitude: newLatitude
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify the CarGPS was restored and location updated
        var restoredCarGPS = await _dbContext.CarGPSes.FirstOrDefaultAsync(c =>
            c.CarId == car.Id && c.DeviceId == device.Id
        );
        Assert.NotNull(restoredCarGPS);
        Assert.False(restoredCarGPS.IsDeleted);
        Assert.Equal(newLatitude, restoredCarGPS.Location.Y, 6);
        Assert.Equal(newLongitude, restoredCarGPS.Location.X, 6);
    }

    [Fact]
    public async Task Handle_ExistingDeviceNotAvailable_ReturnsError()
    {
        // Arrange
        var (car, _) = await SetupTestCar();

        // Create an existing device with InUsed status
        string existingOsBuildId = "BUSY-DEVICE";
        var existingDevice = new GPSDevice
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OSBuildId = existingOsBuildId,
            Name = "Busy Device",
            Status = DeviceStatusEnum.InUsed, // Device is not available
            IsDeleted = false,
        };

        await _dbContext.GPSDevices.AddAsync(existingDevice);
        await _dbContext.SaveChangesAsync();

        var handler = new AssignDeviceToCar.Handler(_dbContext, _geometryFactory);
        var command = new AssignDeviceToCar.Command(
            CarId: car.Id,
            OSBuildId: existingOsBuildId,
            DeviceName: "Updated Device Name",
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.GPSDeviceIsNotAvailable, result.Errors);

        // Verify no CarGPS was created
        var carGPSCount = await _dbContext.CarGPSes.CountAsync(c => c.CarId == car.Id);
        Assert.Equal(0, carGPSCount);

        // Verify device status hasn't changed
        var unchangedDevice = await _dbContext.GPSDevices.FindAsync(existingDevice.Id);
        Assert.NotNull(unchangedDevice);
        Assert.Equal(DeviceStatusEnum.InUsed, unchangedDevice.Status);
    }

    [Fact]
    public async Task Handle_UpdatesDeviceStatusToInUsed_WhenAssigned()
    {
        // Arrange
        var (car, _) = await SetupTestCar();

        // Create an available device
        string deviceOsBuildId = "AVAILABLE-DEVICE";
        var availableDevice = new GPSDevice
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OSBuildId = deviceOsBuildId,
            Name = "Available Device",
            Status = DeviceStatusEnum.Available, // Device is available
            IsDeleted = false,
        };

        await _dbContext.GPSDevices.AddAsync(availableDevice);
        await _dbContext.SaveChangesAsync();

        var handler = new AssignDeviceToCar.Handler(_dbContext, _geometryFactory);
        var command = new AssignDeviceToCar.Command(
            CarId: car.Id,
            OSBuildId: deviceOsBuildId,
            DeviceName: "Device Name",
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify device status is changed to InUsed after assignment
        var updatedDevice = await _dbContext.GPSDevices.FindAsync(availableDevice.Id);
        Assert.NotNull(updatedDevice);
        Assert.Equal(DeviceStatusEnum.InUsed, updatedDevice.Status);

        // Verify CarGPS was created
        var carGPS = await _dbContext.CarGPSes.FirstOrDefaultAsync(c =>
            c.CarId == car.Id && c.DeviceId == availableDevice.Id
        );
        Assert.NotNull(carGPS);
    }

    [Fact]
    public async Task Handle_UpdatesExistingAssociationWithDifferentDevice_Succeeds()
    {
        // Arrange
        var (car, _) = await SetupTestCar();

        // Create first device and associate with car
        var firstDevice = new GPSDevice
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OSBuildId = "FIRST-DEVICE",
            Name = "First Device",
            Status = DeviceStatusEnum.InUsed,
            IsDeleted = false,
        };
        await _dbContext.GPSDevices.AddAsync(firstDevice);

        var initialLocation = _geometryFactory.CreatePoint(new Coordinate(106.6, 10.6));
        initialLocation.SRID = 4326;

        var existingCarGPS = new CarGPS
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            DeviceId = firstDevice.Id,
            Location = initialLocation,
            IsDeleted = false,
        };
        await _dbContext.CarGPSes.AddAsync(existingCarGPS);

        // Create second device for replacement
        var secondDevice = new GPSDevice
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OSBuildId = "SECOND-DEVICE",
            Name = "Second Device",
            Status = DeviceStatusEnum.Available,
            IsDeleted = false,
        };
        await _dbContext.GPSDevices.AddAsync(secondDevice);
        await _dbContext.SaveChangesAsync();

        // Set up handler with second device
        var handler = new AssignDeviceToCar.Handler(_dbContext, _geometryFactory);
        var command = new AssignDeviceToCar.Command(
            CarId: car.Id,
            OSBuildId: "SECOND-DEVICE",
            DeviceName: "Second Device",
            Longtitude: 107.0,
            Latitude: 11.0
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify the association was updated
        var updatedCarGPS = await _dbContext.CarGPSes.FirstOrDefaultAsync(c => c.CarId == car.Id);
        Assert.NotNull(updatedCarGPS);
        Assert.Equal(secondDevice.Id, updatedCarGPS.DeviceId);
        Assert.Equal(11.0, updatedCarGPS.Location.Y, 6);
        Assert.Equal(107.0, updatedCarGPS.Location.X, 6);

        // Verify SRID was set correctly
        Assert.Equal(4326, updatedCarGPS.Location.SRID);

        // Verify second device is now in use
        var updatedDevice = await _dbContext.GPSDevices.FindAsync(secondDevice.Id);
        Assert.NotNull(updatedDevice);
        Assert.Equal(DeviceStatusEnum.InUsed, updatedDevice.Status);
    }

    [Fact]
    public async Task Handle_UpdatesLocationForSameDevice_Succeeds()
    {
        // Arrange
        var (car, _) = await SetupTestCar();

        // Create device and associate with car
        var device = new GPSDevice
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OSBuildId = "LOCATION-TEST",
            Name = "Location Test Device",
            Status = DeviceStatusEnum.Available,
            IsDeleted = false,
        };
        await _dbContext.GPSDevices.AddAsync(device);

        var initialLocation = _geometryFactory.CreatePoint(new Coordinate(106.6, 10.6));
        initialLocation.SRID = 4326;

        var existingCarGPS = new CarGPS
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            DeviceId = device.Id,
            Location = initialLocation,
            IsDeleted = false,
        };
        await _dbContext.CarGPSes.AddAsync(existingCarGPS);
        await _dbContext.SaveChangesAsync();

        // Set up handler with same device but different location
        double newLongitude = 107.5;
        double newLatitude = 11.5;

        var handler = new AssignDeviceToCar.Handler(_dbContext, _geometryFactory);
        var command = new AssignDeviceToCar.Command(
            CarId: car.Id,
            OSBuildId: "LOCATION-TEST", // Same device
            DeviceName: "Location Test Device",
            Longtitude: newLongitude,
            Latitude: newLatitude
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarGPSIsExisted, result.Errors);

        // Verify the location wasn't updated (since the same device is already assigned)
        var unchangedCarGPS = await _dbContext.CarGPSes.FirstOrDefaultAsync(c => c.CarId == car.Id);
        Assert.NotNull(unchangedCarGPS);
        Assert.Equal(10.6, unchangedCarGPS.Location.Y, 6);
        Assert.Equal(106.6, unchangedCarGPS.Location.X, 6);
    }

    [Fact]
    public void Validator_ValidInput_PassesValidation()
    {
        // Arrange
        var validator = new AssignDeviceToCar.Validator();
        var command = new AssignDeviceToCar.Command(
            CarId: Guid.NewGuid(),
            OSBuildId: "TESTBUILD123",
            DeviceName: "Test Device",
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_EmptyCarId_FailsValidation()
    {
        // Arrange
        var validator = new AssignDeviceToCar.Validator();
        var command = new AssignDeviceToCar.Command(
            CarId: Guid.Empty,
            OSBuildId: "TESTBUILD123",
            DeviceName: "Test Device",
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CarId).WithErrorMessage("Phải chọn 1 xe !");
    }

    [Fact]
    public void Validator_EmptyOSBuildId_FailsValidation()
    {
        // Arrange
        var validator = new AssignDeviceToCar.Validator();
        var command = new AssignDeviceToCar.Command(
            CarId: Guid.NewGuid(),
            OSBuildId: string.Empty,
            DeviceName: "Test Device",
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.OSBuildId)
            .WithErrorMessage("ID bản dựng hệ điều hành của thiết bị không được để trống !");
    }

    [Fact]
    public void Validator_EmptyDeviceName_FailsValidation()
    {
        // Arrange
        var validator = new AssignDeviceToCar.Validator();
        var command = new AssignDeviceToCar.Command(
            CarId: Guid.NewGuid(),
            OSBuildId: "TESTBUILD123",
            DeviceName: string.Empty,
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.DeviceName)
            .WithErrorMessage("Tên thiết bị không được để trống !");
    }

    [Fact]
    public void Validator_MultipleInvalidFields_ReturnsMultipleErrors()
    {
        // Arrange
        var validator = new AssignDeviceToCar.Validator();
        var command = new AssignDeviceToCar.Command(
            CarId: Guid.Empty,
            OSBuildId: string.Empty,
            DeviceName: string.Empty,
            Longtitude: 106.7004238,
            Latitude: 10.7756587
        );

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CarId);
        result.ShouldHaveValidationErrorFor(x => x.OSBuildId);
        result.ShouldHaveValidationErrorFor(x => x.DeviceName);
    }

    #region Helper Methods

    private async Task<(Car car, User owner)> SetupTestCar()
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

        // Create encryption key
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(_dbContext);

        // Create pickup location
        var pickupLocation = _geometryFactory.CreatePoint(new Coordinate(106.7004238, 10.7756587));
        pickupLocation.SRID = 4326;

        // Create car
        var car = new Car
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            OwnerId = owner.Id,
            ModelId = model.Id,
            EncryptionKeyId = encryptionKey.Id,
            EncryptedLicensePlate = "TEST-12345",
            FuelTypeId = fuelType.Id,
            TransmissionTypeId = transmissionType.Id,
            Status = CarStatusEnum.Available,
            Color = "Red",
            Seat = 4,
            Description = "Test car description",
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
