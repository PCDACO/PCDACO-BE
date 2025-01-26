using Ardalis.Result;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.UC_Car.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Car.Commands;

public class DeleteCarTests : DatabaseTestBase
{
    [Fact]
    public async Task Handle_UserIsAdmin_ReturnsForbidden()
    {
        // Arrange
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Admin);
        _currentUser.SetUser(user);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testCar = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            user.Id,
            testManufacturer.Id
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
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Driver);
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
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Owner);
        var requester = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Driver);
        _currentUser.SetUser(requester);

        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testCar = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            owner.Id,
            testManufacturer.Id
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
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Owner);
        _currentUser.SetUser(user);
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testCar = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            user.Id,
            testManufacturer.Id
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
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Owner);
        _currentUser.SetUser(user);

        // Create and immediately delete a car
        var testManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var testCar = await TestDataCreateCar.CreateTestCar(
            _dbContext,
            user.Id,
            testManufacturer.Id,
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
