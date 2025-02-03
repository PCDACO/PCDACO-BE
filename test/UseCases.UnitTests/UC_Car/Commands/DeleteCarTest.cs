using Ardalis.Result;
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
        CarStatus carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");

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
            carStatus: carStatus
        );

        var handler = new DeleteCar.Handler(_dbContext, _currentUser);
        var command = new DeleteCar.Command(testCar.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này", result.Errors);
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
        Assert.Contains("Không tìm thấy xe cần xóa", result.Errors);
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
        CarStatus carStatus = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");

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
            carStatus: carStatus
        );

        var handler = new DeleteCar.Handler(_dbContext, _currentUser);
        var command = new DeleteCar.Command(testCar.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền xóa xe này", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_DeletesCarSuccessfully()
    {
        // Arrange
        UserRole ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        TransmissionType transmissionType =
            await TestDataTransmissionType.CreateTestTransmissionType(_dbContext, "Automatic");
        FuelType fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");
        CarStatus status = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");

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
            carStatus: status
        );

        var handler = new DeleteCar.Handler(_dbContext, _currentUser);
        var command = new DeleteCar.Command(testCar.Id);

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
        CarStatus status = await TestDataCarStatus.CreateTestCarStatus(_dbContext, "Available");

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
            carStatus: status,
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
        Assert.Contains("Không tìm thấy xe cần xóa", result.Errors);
    }
}
