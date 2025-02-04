using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_Model.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Model.Commands;

[Collection("Test Collection")]
public class CreateModelTest(DatabaseTestBase fixture) : IAsyncLifetime
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

        var handler = new CreateModel.Handler(_dbContext, _currentUser);
        var command = new CreateModel.Command(
            "Test Model",
            DateTime.UtcNow.AddDays(1),
            Guid.NewGuid()
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này !", result.Errors);
    }

    [Fact]
    public async Task Handle_ManufacturerNotFound_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new CreateModel.Handler(_dbContext, _currentUser);
        var command = new CreateModel.Command(
            "Test Model",
            DateTime.UtcNow.AddDays(1),
            Guid.NewGuid()
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Hãng xe không tồn tại", result.Errors);
    }

    [Fact]
    public async Task Handle_ModelAlreadyExists_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var existingModel = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);

        var handler = new CreateModel.Handler(_dbContext, _currentUser);
        var command = new CreateModel.Command(
            existingModel.Name,
            DateTime.UtcNow.AddDays(1),
            manufacturer.Id
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains($"Mô hình xe đã tồn tại trong hãng xe {manufacturer.Name}", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesModelSuccessfully()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);

        var handler = new CreateModel.Handler(_dbContext, _currentUser);
        var command = new CreateModel.Command(
            "New Model",
            DateTime.UtcNow.AddDays(1),
            manufacturer.Id
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Tạo mô hình xe thành công", result.SuccessMessage);

        // Verify database state
        var createdModel = await _dbContext.Models.FirstOrDefaultAsync(m => m.Name == "New Model");
        Assert.NotNull(createdModel);
        Assert.Equal(manufacturer.Id, createdModel.ManufacturerId);
    }

    [Fact]
    public async Task Validator_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new CreateModel.Validator();
        var command = new CreateModel.Command(
            "", // Empty name
            DateTime.UtcNow.AddDays(-1), // Past date
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
