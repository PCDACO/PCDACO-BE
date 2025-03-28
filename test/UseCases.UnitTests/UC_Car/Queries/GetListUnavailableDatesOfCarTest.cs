using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.UC_Car.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Car.Queries;

[Collection("Test Collection")]
public class GetListUnavailableDatesOfCarTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

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
    public async Task Handle_ReturnsOnlyUnavailableDates()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
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

        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);
        var dayAfter = today.AddDays(2);

        // Create mix of available and unavailable dates
        await _dbContext.CarAvailabilities.AddRangeAsync(
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = today,
                IsAvailable = false, // Unavailable
            },
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = tomorrow,
                IsAvailable = true, // Available
            },
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = dayAfter,
                IsAvailable = false, // Unavailable
            }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.Count); // Only 2 unavailable dates

        // Verify only unavailable dates are returned
        Assert.Contains(result.Value, d => d.Date.Date == today.Date);
        Assert.Contains(result.Value, d => d.Date.Date == dayAfter.Date);
        Assert.DoesNotContain(result.Value, d => d.Date.Date == tomorrow.Date);

        // Verify all returned dates have IsAvailable = false
        Assert.All(result.Value, d => Assert.False(d.IsAvailable));
    }

    [Fact]
    public async Task Handle_FiltersCorrectlyByMonth()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
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

        // Create unavailable dates in different months
        var januaryDate = new DateTimeOffset(DateTime.Now.Year, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var februaryDate = new DateTimeOffset(DateTime.Now.Year, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var marchDate = new DateTimeOffset(DateTime.Now.Year, 3, 15, 0, 0, 0, TimeSpan.Zero);

        await _dbContext.CarAvailabilities.AddRangeAsync(
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = januaryDate,
                IsAvailable = false,
            },
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = februaryDate,
                IsAvailable = false,
            },
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = marchDate,
                IsAvailable = false,
            }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(car.Id, Month: 2); // February only

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value);
        Assert.Equal(2, result.Value.First().Date.Month);
        Assert.Equal(15, result.Value.First().Date.Day);
    }

    [Fact]
    public async Task Handle_FiltersCorrectlyByYear()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
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

        // Create unavailable dates in different years
        var date2022 = new DateTimeOffset(2022, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var date2023 = new DateTimeOffset(2023, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var date2024 = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);

        await _dbContext.CarAvailabilities.AddRangeAsync(
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = date2022,
                IsAvailable = false,
            },
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = date2023,
                IsAvailable = false,
            },
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = date2024,
                IsAvailable = false,
            }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(car.Id, Year: 2023);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value);
        Assert.Equal(2023, result.Value.First().Date.Year);
    }

    [Fact]
    public async Task Handle_FiltersCorrectlyByMonthAndYear()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
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

        // Create unavailable dates in different months and years
        var date1 = new DateTimeOffset(2023, 5, 15, 0, 0, 0, TimeSpan.Zero);
        var date2 = new DateTimeOffset(2023, 6, 15, 0, 0, 0, TimeSpan.Zero); // Target
        var date3 = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);

        await _dbContext.CarAvailabilities.AddRangeAsync(
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = date1,
                IsAvailable = false,
            },
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = date2,
                IsAvailable = false,
            },
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = date3,
                IsAvailable = false,
            }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(car.Id, Month: 6, Year: 2023);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value);
        Assert.Equal(6, result.Value.First().Date.Month);
        Assert.Equal(2023, result.Value.First().Date.Year);
    }

    [Fact]
    public async Task Handle_WithOnlyMonth_DefaultsToCurrentYear()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
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

        int currentYear = DateTimeOffset.UtcNow.Year;
        int lastYear = currentYear - 1;

        // Create unavailable dates for April in current year and last year
        var dateCurrentYear = new DateTimeOffset(currentYear, 4, 15, 0, 0, 0, TimeSpan.Zero); // Should be included
        var dateLastYear = new DateTimeOffset(lastYear, 4, 15, 0, 0, 0, TimeSpan.Zero); // Should be excluded

        await _dbContext.CarAvailabilities.AddRangeAsync(
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = dateCurrentYear,
                IsAvailable = false,
            },
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = dateLastYear,
                IsAvailable = false,
            }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(car.Id, Month: 4); // April only, no year specified

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value);
        Assert.Equal(4, result.Value.First().Date.Month);
        Assert.Equal(currentYear, result.Value.First().Date.Year);
    }

    [Fact]
    public async Task Handle_WithInvalidMonth_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
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

        // Test with month = 0
        var query1 = new GetListUnavailableDatesOfCar.Query(car.Id, Month: 0);
        var result1 = await handler.Handle(query1, CancellationToken.None);

        // Test with month = 13
        var query2 = new GetListUnavailableDatesOfCar.Query(car.Id, Month: 13);
        var result2 = await handler.Handle(query2, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result1.Status);
        Assert.Contains("Tháng không hợp lệ", result1.Errors);

        Assert.Equal(ResultStatus.Error, result2.Status);
        Assert.Contains("Tháng không hợp lệ", result2.Errors);
    }

    [Fact]
    public async Task Handle_DoesNotReturnDeletedRecords()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
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

        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);

        var availability1 = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = today,
            IsAvailable = false,
        };

        var availability2 = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = tomorrow,
            IsAvailable = false,
        };

        await _dbContext.CarAvailabilities.AddRangeAsync(availability1, availability2);
        await _dbContext.SaveChangesAsync();

        // Mark one record as deleted
        availability1.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        var handler = new GetListUnavailableDatesOfCar.Handler(_dbContext);
        var query = new GetListUnavailableDatesOfCar.Query(car.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value); // Only one non-deleted record
        Assert.Equal(tomorrow.Date, result.Value.First().Date.Date);
    }

    [Fact]
    public async Task Handle_DoesNotReturnDatesForDeletedCar()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
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

        // Create unavailable date
        await _dbContext.CarAvailabilities.AddAsync(
            new CarAvailability
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                CarId = car.Id,
                Date = DateTimeOffset.UtcNow,
                IsAvailable = false,
            }
        );
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
