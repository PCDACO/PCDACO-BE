using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_Car.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Car.Queries;

[Collection("Test Collection")]
public class GetListUnavailableDatesOfCarTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_CarWithUnavailableDates_ReturnsListOfDates()
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

        // Create three unavailable date records
        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);
        var dayAfterTomorrow = today.AddDays(2);

        var todayAvailability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = today,
            IsAvailable = false,
        };

        var tomorrowAvailability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = tomorrow,
            IsAvailable = false,
        };

        var dayAfterTomorrowAvailability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = dayAfterTomorrow,
            IsAvailable = false,
        };

        await _dbContext.CarAvailabilities.AddRangeAsync(
            todayAvailability,
            tomorrowAvailability,
            dayAfterTomorrowAvailability
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.Count);

        // Check if the dates are correctly returned
        Assert.Contains(result.Value, d => d.Date.Date == today.Date);
        Assert.Contains(result.Value, d => d.Date.Date == tomorrow.Date);
        Assert.Contains(result.Value, d => d.Date.Date == dayAfterTomorrow.Date);

        // Check that all dates are marked as unavailable
        Assert.All(result.Value, d => Assert.False(d.IsAvailable));
    }

    [Fact]
    public async Task Handle_CarWithNoUnavailableDates_ReturnsEmptyList()
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

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_AvailableDatesExist_DoesNotReturnAvailableDates()
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

        // Create one unavailable date and one available date
        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);

        var todayAvailability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = today,
            IsAvailable = false, // Unavailable
        };

        var tomorrowAvailability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = tomorrow,
            IsAvailable = true, // Available - should not be in results
        };

        await _dbContext.CarAvailabilities.AddRangeAsync(todayAvailability, tomorrowAvailability);
        await _dbContext.SaveChangesAsync();

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value);
        Assert.Equal(today.Date, result.Value[0].Date.Date);
        Assert.False(result.Value[0].IsAvailable);
    }

    [Fact]
    public async Task Handle_DeletedRecordsExist_DoesNotReturnDeletedRecords()
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

        // Create one active record and one deleted record
        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);

        var todayAvailability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = today,
            IsAvailable = false,
        };

        var tomorrowAvailability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = tomorrow,
            IsAvailable = false,
            IsDeleted = true, // Deleted - should not be in results
        };

        await _dbContext.CarAvailabilities.AddRangeAsync(todayAvailability, tomorrowAvailability);
        await _dbContext.SaveChangesAsync();

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value);
        Assert.Equal(today.Date, result.Value[0].Date.Date);
    }

    [Fact]
    public async Task Handle_MultipleCarAvailabilities_OnlyReturnsForRequestedCar()
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

        // Create two cars
        var car1 = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var car2 = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        // Create unavailable dates for both cars
        var today = DateTimeOffset.UtcNow;

        var car1Availability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car1.Id,
            Date = today,
            IsAvailable = false,
        };

        var car2Availability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car2.Id,
            Date = today,
            IsAvailable = false,
        };

        await _dbContext.CarAvailabilities.AddRangeAsync(car1Availability, car2Availability);
        await _dbContext.SaveChangesAsync();

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(car1.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value);
        Assert.Equal(today.Date, result.Value[0].Date.Date);
    }

    [Fact]
    public async Task Handle_CarNotFound_ReturnsNotFound()
    {
        // Arrange
        var nonExistentCarId = Guid.NewGuid();

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(nonExistentCarId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.CarNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_DeletedCar_ReturnsNotFound()
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

        // Create a car and mark it as deleted
        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        // Create unavailable date
        var availability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = DateTimeOffset.UtcNow,
            IsAvailable = false,
        };
        await _dbContext.CarAvailabilities.AddAsync(availability);
        await _dbContext.SaveChangesAsync();

        // Mark car as deleted
        car.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.CarNotFound, result.Errors);
    }
}
