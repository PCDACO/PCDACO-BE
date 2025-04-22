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
public class UnassignGPSDeviceForCarTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ValidRequest_PendingCar_Succeeds()
    {
        // Arrange
        var (car, _) = await SetupTestCarWithStatus(CarStatusEnum.Pending);

        // Create GPS device
        var device = await TestDataGPSDevice.CreateTestGPSDevice(
            _dbContext,
            status: DeviceStatusEnum.InUsed
        );

        // Create CarGPS association
        var carGPS = await CreateCarGPSAssociation(car.Id, device.Id);

        // Create an in progress ChangeGPS inspection schedule for the car
        await CreateChangeGPSInspectionSchedule(car.Id);

        // Create handler and command
        var handler = new UnassignGPSDeviceForCar.Handler(_dbContext);
        var command = new UnassignGPSDeviceForCar.Command(device.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Gỡ thiết bị GPS khỏi xe thành công", result.SuccessMessage);

        // Verify device status was updated to Available
        var updatedDevice = await _dbContext.GPSDevices.FindAsync(device.Id);
        Assert.NotNull(updatedDevice);
        Assert.Equal(DeviceStatusEnum.Available, updatedDevice.Status);

        // Verify CarGPS association was removed
        var deletedCarGPS = await _dbContext
            .CarGPSes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CarId == car.Id && c.DeviceId == device.Id);
        Assert.Null(deletedCarGPS);
    }

    [Fact]
    public async Task Handle_ValidRequest_DeletedCar_Succeeds()
    {
        // Arrange
        var (car, _) = await SetupTestCarWithStatus(CarStatusEnum.Available, isDeleted: true);

        // Create GPS device
        var device = await TestDataGPSDevice.CreateTestGPSDevice(
            _dbContext,
            status: DeviceStatusEnum.InUsed
        );

        // Create CarGPS association
        var carGPS = await CreateCarGPSAssociation(car.Id, device.Id);

        // Create an in progress ChangeGPS inspection schedule for the car
        await CreateChangeGPSInspectionSchedule(car.Id);

        // Create handler and command
        var handler = new UnassignGPSDeviceForCar.Handler(_dbContext);
        var command = new UnassignGPSDeviceForCar.Command(device.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Gỡ thiết bị GPS khỏi xe thành công", result.SuccessMessage);

        // Verify device status was updated to Available
        var updatedDevice = await _dbContext.GPSDevices.FindAsync(device.Id);
        Assert.NotNull(updatedDevice);
        Assert.Equal(DeviceStatusEnum.Available, updatedDevice.Status);

        // Verify CarGPS association was removed
        var deletedCarGPS = await _dbContext
            .CarGPSes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CarId == car.Id && c.DeviceId == device.Id);
        Assert.Null(deletedCarGPS);
    }

    [Fact]
    public async Task Handle_NoChangeGPSSchedule_ReturnsError()
    {
        // Arrange
        var (car, _) = await SetupTestCarWithStatus(CarStatusEnum.Pending);

        // Create GPS device
        var device = await TestDataGPSDevice.CreateTestGPSDevice(
            _dbContext,
            status: DeviceStatusEnum.InUsed
        );

        // Create CarGPS association without inspection schedule
        var carGPS = await CreateCarGPSAssociation(car.Id, device.Id);

        // Create handler and command
        var handler = new UnassignGPSDeviceForCar.Handler(_dbContext);
        var command = new UnassignGPSDeviceForCar.Command(device.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Xe không có lịch đổi thiết bị gps nào đang diễn ra", result.Errors);
    }

    [Fact]
    public async Task Handle_GPSDeviceNotFound_ReturnsError()
    {
        // Arrange
        var nonExistentDeviceId = Guid.NewGuid();

        var handler = new UnassignGPSDeviceForCar.Handler(_dbContext);
        var command = new UnassignGPSDeviceForCar.Command(nonExistentDeviceId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.GPSDeviceNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_DeletedGPSDevice_ReturnsError()
    {
        // Arrange
        var device = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext, isDeleted: true);

        var handler = new UnassignGPSDeviceForCar.Handler(_dbContext);
        var command = new UnassignGPSDeviceForCar.Command(device.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.GPSDeviceNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_DeviceNotAssociatedWithCar_ReturnsNotFound()
    {
        // Arrange
        var device = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext);

        var handler = new UnassignGPSDeviceForCar.Handler(_dbContext);
        var command = new UnassignGPSDeviceForCar.Command(device.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Thiết bị GPS không được gán cho xe nào", result.Errors);
    }

    [Fact]
    public void Validator_EmptyGPSDeviceId_FailsValidation()
    {
        // Arrange
        var validator = new UnassignGPSDeviceForCar.Validator();
        var command = new UnassignGPSDeviceForCar.Command(Guid.Empty);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.GPSDeviceId)
            .WithErrorMessage("ID thiết bị GPS không được để trống");
    }

    #region Helper Methods

    private async Task<(Car car, User owner)> SetupTestCarWithStatus(
        CarStatusEnum status,
        bool isDeleted = false
    )
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
            Status = status,
            Color = "Red",
            Seat = 4,
            Description = "Test Car",
            FuelConsumption = 7.5m,
            Price = 100m,
            RequiresCollateral = false,
            Terms = "Standard terms",
            PickupLocation = pickupLocation,
            PickupAddress = "Test Address",
            IsDeleted = isDeleted,
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

    private async Task<CarGPS> CreateCarGPSAssociation(Guid carId, Guid deviceId)
    {
        var location = _geometryFactory.CreatePoint(new Coordinate(106.7004238, 10.7756587));
        location.SRID = 4326;

        var carGPS = new CarGPS
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = carId,
            DeviceId = deviceId,
            Location = location,
            IsDeleted = false,
        };

        await _dbContext.CarGPSes.AddAsync(carGPS);
        await _dbContext.SaveChangesAsync();

        return carGPS;
    }

    private async Task<InspectionSchedule> CreateChangeGPSInspectionSchedule(Guid carId)
    {
        // Create technician role
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );

        // Create technician
        var technician = await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);

        // Create consultant role
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );

        // Create consultant
        var consultant = await TestDataCreateUser.CreateTestUser(_dbContext, consultantRole);

        // Create inspection schedule
        var inspectionSchedule = new InspectionSchedule
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = carId,
            TechnicianId = technician.Id,
            CreatedBy = consultant.Id,
            Status = InspectionScheduleStatusEnum.InProgress,
            Type = InspectionScheduleType.ChangeGPS,
            InspectionAddress = "Test Address",
            InspectionDate = DateTimeOffset.UtcNow.AddDays(1),
            IsDeleted = false,
        };

        await _dbContext.InspectionSchedules.AddAsync(inspectionSchedule);
        await _dbContext.SaveChangesAsync();

        return inspectionSchedule;
    }

    #endregion
}
