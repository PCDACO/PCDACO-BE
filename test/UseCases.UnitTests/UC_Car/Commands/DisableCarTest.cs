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

namespace UseCases.UnitTests.UC_Car.Commands;

[Collection("Test Collection")]
public class DisableCarTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ValidRequest_DisablesCarSuccessfully()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var user = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(user);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        // Create an available car
        var availableCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: user.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var handler = new DisableCar.Handler(_dbContext, _currentUser);
        var command = new DisableCar.Command(availableCar.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify car status changed to Inactive
        var updatedCar = await _dbContext
            .Cars.Where(c => c.Id == availableCar.Id)
            .SingleOrDefaultAsync();

        Assert.NotNull(updatedCar);
        Assert.Equal(CarStatusEnum.Inactive, updatedCar.Status);
        Assert.Equal(availableCar.Id, result.Value.Id);
        Assert.Equal("Inactive", result.Value.Status);
    }

    [Fact]
    public async Task Handle_UserIsAdmin_ReturnsForbidden()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        // Create owner for the car
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var carOwner = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "carowner@test.com"
        );

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var availableCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: carOwner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var handler = new DisableCar.Handler(_dbContext, _currentUser);
        var command = new DisableCar.Command(availableCar.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_CarNotFound_ReturnsNotFound()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var nonExistentCarId = Guid.NewGuid();

        var handler = new DisableCar.Handler(_dbContext, _currentUser);
        var command = new DisableCar.Command(nonExistentCarId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
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

        // Set current user to the different owner (not the car owner)
        _currentUser.SetUser(differentOwner);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var availableCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: realOwner.Id, // Car belongs to realOwner, not differentOwner
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var handler = new DisableCar.Handler(_dbContext, _currentUser);
        var command = new DisableCar.Command(availableCar.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_CarNotInAvailableStatus_ReturnsError()
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

        // Create car with Pending status (not Available)
        var pendingCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Pending // Car is not in Available status
        );

        var handler = new DisableCar.Handler(_dbContext, _currentUser);
        var command = new DisableCar.Command(pendingCar.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarMustBeAvailableToBeDisabled, result.Errors);

        // Verify car status remains Pending
        var unchangedCar = await _dbContext
            .Cars.Where(c => c.Id == pendingCar.Id)
            .SingleOrDefaultAsync();

        Assert.NotNull(unchangedCar);
        Assert.Equal(CarStatusEnum.Pending, unchangedCar.Status);
    }

    [Fact]
    public async Task Handle_CarHasActiveBookings_ReturnsConflict()
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

        var availableCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        // Create an active booking for the car
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CarId = availableCar.Id,
            UserId = driver.Id,
            Status = BookingStatusEnum.Pending,
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(2),
            ActualReturnTime = DateTimeOffset.UtcNow.AddDays(2),
            BasePrice = 100m,
            PlatformFee = 10m,
            ExcessDay = 0,
            ExcessDayFee = 0m,
            TotalAmount = 110m,
            Note = "Test booking",
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var handler = new DisableCar.Handler(_dbContext, _currentUser);
        var command = new DisableCar.Command(availableCar.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains(ResponseMessages.CarHasActiveBookings, result.Errors);

        // Verify car status remains Available
        var unchangedCar = await _dbContext
            .Cars.Where(c => c.Id == availableCar.Id)
            .SingleOrDefaultAsync();

        Assert.NotNull(unchangedCar);
        Assert.Equal(CarStatusEnum.Available, unchangedCar.Status);
    }

    [Fact]
    public async Task Handle_UpdatesTimestamp_WhenCarDisabled()
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

        var availableCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        // Record original timestamp
        var originalTimestamp = availableCar.UpdatedAt;

        // Wait a moment to ensure timestamps are different
        await Task.Delay(10);

        var handler = new DisableCar.Handler(_dbContext, _currentUser);
        var command = new DisableCar.Command(availableCar.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedCar = await _dbContext
            .Cars.Where(c => c.Id == availableCar.Id)
            .SingleOrDefaultAsync();

        Assert.NotNull(updatedCar);
        Assert.NotEqual(originalTimestamp, updatedCar.UpdatedAt);
    }
}
