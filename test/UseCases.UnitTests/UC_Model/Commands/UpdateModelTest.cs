using Ardalis.Result;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_Model.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Model.Commands;

[Collection("Test Collection")]
public class UpdateModelTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsError()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new UpdateModel.Handler(_dbContext, _currentUser);
        var command = new UpdateModel.Command(
            Guid.NewGuid(),
            "Updated Model",
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này !", result.Errors);
    }

    [Fact]
    public async Task Handle_ModelNotFound_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new UpdateModel.Handler(_dbContext, _currentUser);
        var command = new UpdateModel.Command(
            Guid.NewGuid(),
            "Updated Model",
            DateTimeOffset.UtcNow,
            Guid.NewGuid()
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Mô hình xe không tồn tại", result.Errors);
    }

    [Fact]
    public async Task Handle_ManufacturerNotFound_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var handler = new UpdateModel.Handler(_dbContext, _currentUser);
        var command = new UpdateModel.Command(
            model.Id,
            "Updated Model",
            DateTimeOffset.UtcNow,
            Guid.NewGuid() // Non-existent manufacturer ID
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Hãng xe không tồn tại", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesModelSuccessfully()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var newManufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var updateTime = DateTimeOffset.UtcNow;
        var handler = new UpdateModel.Handler(_dbContext, _currentUser);
        var command = new UpdateModel.Command(
            model.Id,
            "Updated Model",
            updateTime,
            newManufacturer.Id
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Cập nhật mô hình xe thành công", result.SuccessMessage);

        // Verify database updates
        var updatedModel = await _dbContext.Models.FirstAsync(m => m.Id == model.Id);
        Assert.Equal("Updated Model", updatedModel.Name);
        Assert.Equal(updateTime, updatedModel.ReleaseDate);
        Assert.Equal(newManufacturer.Id, updatedModel.ManufacturerId);
        Assert.True(DateTimeOffset.UtcNow >= updatedModel.UpdatedAt);
    }

    [Fact]
    public void Validator_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new UpdateModel.Validator();
        var command = new UpdateModel.Command(
            Guid.Empty,
            "", // Empty name
            default, // Empty release date
            Guid.Empty // Empty manufacturer ID
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "ReleaseDate");
        Assert.Contains(result.Errors, e => e.PropertyName == "ManufacturerId");
    }
}
