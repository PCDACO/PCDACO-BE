using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_Car.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Car.Commands;

[Collection("Test Collection")]
public class SetCarUnavailabilityTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ValidRequest_CreatesAvailabilityRecord()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var date = DateTimeOffset.UtcNow;
        var command = new SetCarUnavailability.Command(car.Id, date, false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains(ResponseMessages.Updated, result.SuccessMessage);

        // Verify record was created
        var availability = await _dbContext
            .CarAvailabilities.Where(c => c.CarId == car.Id && c.Date.Date == date.Date)
            .SingleOrDefaultAsync();

        Assert.NotNull(availability);
        Assert.False(availability.IsAvailable);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesExistingAvailabilityRecord()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        // Create initial availability record (unavailable)
        var date = DateTimeOffset.UtcNow;
        var existingAvailability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = date,
            IsAvailable = false,
        };
        await _dbContext.CarAvailabilities.AddAsync(existingAvailability);
        await _dbContext.SaveChangesAsync();

        // Create handler and command to set to available
        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(car.Id, date, true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains(ResponseMessages.Updated, result.SuccessMessage);

        // Verify record was updated
        var updatedAvailability = await _dbContext
            .CarAvailabilities.Where(c => c.CarId == car.Id && c.Date.Date == date.Date)
            .SingleOrDefaultAsync();

        Assert.NotNull(updatedAvailability);
        Assert.True(updatedAvailability.IsAvailable); // Should now be available
        Assert.Equal(existingAvailability.Id, updatedAvailability.Id); // Should be the same record
    }

    [Fact]
    public async Task Handle_CarNotFound_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var nonExistentCarId = Guid.NewGuid();
        var date = DateTimeOffset.UtcNow.UtcDateTime;

        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(nonExistentCarId, date, false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_UserNotOwner_ReturnsForbidden()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        // Create two different owners
        var realOwner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var differentOwner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "different@test.com"
        );

        // Set current user to differentOwner (not the car owner)
        _currentUser.SetUser(differentOwner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        // Create car owned by realOwner
        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: realOwner.Id, // Car belongs to realOwner, not differentOwner
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var date = DateTimeOffset.UtcNow.UtcDateTime;
        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(car.Id, date, false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_ExistingBookings_ReturnsConflict()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@example.com"
        );
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        // Create a booking that spans the target date
        var targetDate = DateTimeOffset.UtcNow.UtcDateTime;
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CarId = car.Id,
            UserId = driver.Id,
            Status = BookingStatusEnum.Approved,
            StartTime = targetDate.AddDays(-1), // Starts before target date
            EndTime = targetDate.AddDays(1), // Ends after target date
            ActualReturnTime = targetDate.AddDays(1),
            BasePrice = 100m,
            PlatformFee = 10m,
            ExcessDay = 0,
            ExcessDayFee = 0m,
            TotalAmount = 110m,
            Note = "Test booking",
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(car.Id, targetDate, false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("đã có đơn đặt xe", result.Errors.First()); // The error should mention existing booking
    }

    [Fact]
    public async Task Handle_UpdatesTimestamp_WhenAvailabilityUpdated()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        // Create initial availability record
        var date = DateTimeOffset.UtcNow.UtcDateTime;
        var existingAvailability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = date,
            IsAvailable = false,
        };
        await _dbContext.CarAvailabilities.AddAsync(existingAvailability);
        await _dbContext.SaveChangesAsync();

        // Record original timestamp
        var originalTimestamp = existingAvailability.UpdatedAt;

        // Wait a moment to ensure timestamps are different
        await Task.Delay(10);

        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(car.Id, date, true);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedAvailability = await _dbContext
            .CarAvailabilities.Where(c => c.Id == existingAvailability.Id)
            .SingleOrDefaultAsync();

        Assert.NotNull(updatedAvailability);
        Assert.NotEqual(originalTimestamp, updatedAvailability.UpdatedAt);
    }
}
