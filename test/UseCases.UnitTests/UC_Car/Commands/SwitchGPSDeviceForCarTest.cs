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
    public async Task Handle_DeviceAssociatedWithDeletedCarGPS_CreatesNewAssociation()
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
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify a new CarGPS was created
        var carGPSs = await _dbContext.CarGPSes.Where(c => c.DeviceId == device.Id).ToListAsync();
        Assert.Single(carGPSs.Where(c => !c.IsDeleted)); // One active association

        var activeCarGPS = carGPSs.First(c => !c.IsDeleted);
        Assert.Equal(car.Id, activeCarGPS.CarId);
        Assert.Equal(11.0, activeCarGPS.Location.Y, 6);
        Assert.Equal(107.0, activeCarGPS.Location.X, 6);
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
