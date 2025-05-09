using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_Car.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Car.Commands;

[Collection("Test Collection")]
public class DeleteCarTests(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserIsAdmin_ReturnsForbidden()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");

        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var user = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(user);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testModel = await TestDataCreateModel.CreateTestModel(_dbContext, testManufacturer.Id);
        var testCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: user.Id,
            modelId: testModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: Domain.Enums.CarStatusEnum.Available
        );

        var handler = new DeleteCar.Handler(_dbContext, _currentUser);
        var command = new DeleteCar.Command(testCar.Id);

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
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(user);

        var handler = new DeleteCar.Handler(_dbContext, _currentUser);
        var command = new DeleteCar.Command(Uuid.NewDatabaseFriendly(Database.PostgreSql));

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
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var requester = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(requester);

        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testModel = await TestDataCreateModel.CreateTestModel(_dbContext, testManufacturer.Id);
        var testCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: testModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: Domain.Enums.CarStatusEnum.Available
        );

        var handler = new DeleteCar.Handler(_dbContext, _currentUser);
        var command = new DeleteCar.Command(testCar.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_DeletesCarSuccessfully()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var user = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(user);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testModel = await TestDataCreateModel.CreateTestModel(_dbContext, testManufacturer.Id);
        var testCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: user.Id,
            modelId: testModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: Domain.Enums.CarStatusEnum.Available
        );

        var handler = new DeleteCar.Handler(_dbContext, _currentUser);
        var command = new DeleteCar.Command(testCar.Id);

        // Remove all related carGPS of this car
        var carGPS = await _dbContext.CarGPSes.Where(g => g.CarId == testCar.Id).ToListAsync();
        _dbContext.CarGPSes.RemoveRange(carGPS);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify car soft-delete
        var deletedCar = await _dbContext
            .Cars.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == testCar.Id);

        Assert.NotNull(deletedCar);
        Assert.True(deletedCar!.IsDeleted);
        Assert.NotNull(deletedCar.DeletedAt);

        // Verify related entities soft-delete
        Assert.Empty(
            await _dbContext
                .ImageCars.IgnoreQueryFilters()
                .Where(ic => ic.CarId == testCar.Id && !ic.IsDeleted)
                .ToListAsync()
        );
    }

    [Fact]
    public async Task Handle_CarAlreadyDeleted_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var user = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(user);

        // Create and immediately delete a car
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testModel = await TestDataCreateModel.CreateTestModel(_dbContext, testManufacturer.Id);
        var testCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: user.Id,
            modelId: testModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: Domain.Enums.CarStatusEnum.Available,
            isDeleted: true
        );

        var handler = new DeleteCar.Handler(_dbContext, _currentUser);
        await handler.Handle(new DeleteCar.Command(testCar.Id), CancellationToken.None);

        // Act - Try to delete again
        var result = await handler.Handle(
            new DeleteCar.Command(testCar.Id),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.CarNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_CarWithGpsDevice_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var user = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(user);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testModel = await TestDataCreateModel.CreateTestModel(_dbContext, testManufacturer.Id);

        // Create car with GPS device attached (TestDataCreateCar.CreateTestCar automatically attaches a GPS device)
        var testCar = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: user.Id,
            modelId: testModel.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: Domain.Enums.CarStatusEnum.Available
        );

        // Verify the car has a GPS device attached
        var hasGps = await _dbContext.CarGPSes.AnyAsync(g => g.CarId == testCar.Id);
        Assert.True(hasGps, "Test setup failed: Car should have GPS device attached");

        var handler = new DeleteCar.Handler(_dbContext, _currentUser);
        var command = new DeleteCar.Command(testCar.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Vui lòng gỡ thiết bị gps trước khi xóa xe!", result.Errors);

        // Verify car was not deleted
        var carStillExists = await _dbContext.Cars.AnyAsync(c =>
            c.Id == testCar.Id && !c.IsDeleted
        );
        Assert.True(carStillExists);
    }
}
