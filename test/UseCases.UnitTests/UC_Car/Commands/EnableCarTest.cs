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
public class EnableCarTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ValidRequest_EnablesCarSuccessfully()
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

        // Create an inactive car
        var inactiveCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: user.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Inactive
        );

        var handler = new EnableCar.Handler(_dbContext, _currentUser);
        var command = new EnableCar.Command(inactiveCar.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify car status changed to Available
        var updatedCar = await _dbContext
            .Cars.Where(c => c.Id == inactiveCar.Id)
            .SingleOrDefaultAsync();

        Assert.NotNull(updatedCar);
        Assert.Equal(CarStatusEnum.Available, updatedCar.Status);
        Assert.Equal(inactiveCar.Id, result.Value.Id);
        Assert.Equal("Available", result.Value.Status);
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

        var inactiveCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: carOwner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Inactive
        );

        var handler = new EnableCar.Handler(_dbContext, _currentUser);
        var command = new EnableCar.Command(inactiveCar.Id);

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

        var handler = new EnableCar.Handler(_dbContext, _currentUser);
        var command = new EnableCar.Command(nonExistentCarId);

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

        var inactiveCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: realOwner.Id, // Car belongs to realOwner, not differentOwner
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Inactive
        );

        var handler = new EnableCar.Handler(_dbContext, _currentUser);
        var command = new EnableCar.Command(inactiveCar.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_CarNotInInactiveStatus_ReturnsError()
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

        // Create car with Available status (not Inactive)
        var availableCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available // Car is already available
        );

        var handler = new EnableCar.Handler(_dbContext, _currentUser);
        var command = new EnableCar.Command(availableCar.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarMustBeInactiveToBeEnabled, result.Errors);

        // Verify car status remains Available
        var unchangedCar = await _dbContext
            .Cars.Where(c => c.Id == availableCar.Id)
            .SingleOrDefaultAsync();

        Assert.NotNull(unchangedCar);
        Assert.Equal(CarStatusEnum.Available, unchangedCar.Status);
    }

    [Fact]
    public async Task Handle_UpdatesTimestamp_WhenCarEnabled()
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

        var inactiveCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Inactive
        );

        // Record original timestamp
        var originalTimestamp = inactiveCar.UpdatedAt;

        // Wait a moment to ensure timestamps are different
        await Task.Delay(10);

        var handler = new EnableCar.Handler(_dbContext, _currentUser);
        var command = new EnableCar.Command(inactiveCar.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedCar = await _dbContext
            .Cars.Where(c => c.Id == inactiveCar.Id)
            .SingleOrDefaultAsync();

        Assert.NotNull(updatedCar);
        Assert.NotEqual(originalTimestamp, updatedCar.UpdatedAt);
    }
}
