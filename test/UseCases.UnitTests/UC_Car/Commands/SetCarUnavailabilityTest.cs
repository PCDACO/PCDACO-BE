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
    public async Task Handle_EmptyDatesList_ReturnsError()
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
        var command = new SetCarUnavailability.Command(car.Id, new List<DateTimeOffset>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Ngày không hợp lệ", result.Errors);
    }

    [Fact]
    public async Task Handle_CarNotFound_ReturnsError()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        _currentUser.SetUser(owner);

        var nonExistentCarId = Guid.NewGuid();
        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(
            nonExistentCarId,
            new List<DateTimeOffset> { DateTimeOffset.UtcNow }
        );

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

        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, ownerRole);
        var otherUser = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            ownerRole,
            "other@example.com"
        );
        _currentUser.SetUser(otherUser); // Set current user to someone else

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id, // Real owner
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(
            car.Id,
            new List<DateTimeOffset> { DateTimeOffset.UtcNow }
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_DatesWithExistingBookings_ReturnsConflict()
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

        // Create a booking for tomorrow
        var bookingStartTime = DateTimeOffset.UtcNow.AddDays(1);
        var bookingEndTime = bookingStartTime.AddDays(2);
        var booking = new Booking
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            UserId = driver.Id,
            CarId = car.Id,
            Status = BookingStatusEnum.Approved,
            StartTime = bookingStartTime,
            EndTime = bookingEndTime,
            ActualReturnTime = bookingEndTime,
            BasePrice = 100m,
            PlatformFee = 10m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 110m,
            Note = "Test booking",
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(
            car.Id,
            new List<DateTimeOffset> { bookingStartTime.AddDays(1) } // Date within booking range
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("Không thể thay đổi trạng thái vì ngày", result.Errors.First());
        Assert.Contains("đã có đơn đặt xe", result.Errors.First());
    }

    [Fact]
    public async Task Handle_CreatesNewAvailabilityRecords_WhenNoneExisting()
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

        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);
        var dayAfter = today.AddDays(2);

        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(
            car.Id,
            new List<DateTimeOffset> { today, tomorrow, dayAfter },
            false // Mark as unavailable
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify records were created
        var createdRecords = await _dbContext
            .CarAvailabilities.Where(ca => ca.CarId == car.Id && !ca.IsDeleted)
            .ToListAsync();

        Assert.Equal(3, createdRecords.Count);
        Assert.Contains(createdRecords, ca => ca.Date.Date == today.Date && !ca.IsAvailable);
        Assert.Contains(createdRecords, ca => ca.Date.Date == tomorrow.Date && !ca.IsAvailable);
        Assert.Contains(createdRecords, ca => ca.Date.Date == dayAfter.Date && !ca.IsAvailable);
    }

    [Fact]
    public async Task Handle_UpdatesExistingAvailabilityRecords()
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

        var today = DateTimeOffset.UtcNow;

        // Create existing availability record marked as unavailable
        var existingAvailability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = today,
            IsAvailable = false,
        };

        await _dbContext.CarAvailabilities.AddAsync(existingAvailability);
        await _dbContext.SaveChangesAsync();

        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(
            car.Id,
            new List<DateTimeOffset> { today },
            true // Mark as available (change from current false)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify record was updated
        var updatedRecord = await _dbContext.CarAvailabilities.FirstAsync(ca =>
            ca.Id == existingAvailability.Id
        );

        Assert.True(updatedRecord.IsAvailable);
        Assert.NotNull(updatedRecord.UpdatedAt);
    }

    [Fact]
    public async Task Handle_MixOfUpdateAndCreate_HandlesCorrectly()
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

        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);
        var dayAfter = today.AddDays(2);

        // Create one existing availability record
        var existingAvailability = new CarAvailability
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            CarId = car.Id,
            Date = today,
            IsAvailable = true, // Will be changed to false
        };

        await _dbContext.CarAvailabilities.AddAsync(existingAvailability);
        await _dbContext.SaveChangesAsync();

        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(
            car.Id,
            new List<DateTimeOffset> { today, tomorrow, dayAfter },
            false // Mark all as unavailable
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify records
        var allRecords = await _dbContext
            .CarAvailabilities.Where(ca => ca.CarId == car.Id && !ca.IsDeleted)
            .ToListAsync();

        Assert.Equal(3, allRecords.Count);

        // First record should be updated
        var updatedRecord = allRecords.First(r => r.Date.Date == today.Date);
        Assert.Equal(existingAvailability.Id, updatedRecord.Id);
        Assert.False(updatedRecord.IsAvailable);

        // The other two should be new
        Assert.Contains(allRecords, ca => ca.Date.Date == tomorrow.Date && !ca.IsAvailable);
        Assert.Contains(allRecords, ca => ca.Date.Date == dayAfter.Date && !ca.IsAvailable);
    }

    [Fact]
    public async Task Handle_MakeUnavailableDatesAvailable_Succeeds()
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

        var today = DateTimeOffset.UtcNow;
        var tomorrow = today.AddDays(1);

        // Create existing unavailable dates
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

        await _dbContext.CarAvailabilities.AddRangeAsync(todayAvailability, tomorrowAvailability);
        await _dbContext.SaveChangesAsync();

        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(
            car.Id,
            new List<DateTimeOffset> { today, tomorrow },
            true // Make them available again
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify both records were updated to available
        var records = await _dbContext
            .CarAvailabilities.Where(ca => ca.CarId == car.Id && !ca.IsDeleted)
            .ToListAsync();

        Assert.Equal(2, records.Count);
        Assert.All(records, r => Assert.True(r.IsAvailable));
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

        var car = await TestDataCreateCar.CreateTestCar(
            dBContext: _dbContext,
            ownerId: owner.Id,
            modelId: model.Id,
            transmissionType: transmissionType,
            fuelType: fuelType,
            carStatus: CarStatusEnum.Available
        );

        // Mark car as deleted
        car.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        var handler = new SetCarUnavailability.Handler(_dbContext, _currentUser);
        var command = new SetCarUnavailability.Command(
            car.Id,
            new List<DateTimeOffset> { DateTimeOffset.UtcNow }
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.CarNotFound, result.Errors);
    }
}
