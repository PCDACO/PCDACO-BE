using Ardalis.Result;
using Domain.Constants;
using Domain.Enums;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_GPSDevice.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_GPSDevice.Commands;

[Collection("Test Collection")]
public class UpdateGPSDeviceTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ValidRequestByAdmin_UpdatesDeviceSuccessfully()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        var gpsDevice = await TestDataGPSDevice.CreateTestGPSDevice(
            _dbContext,
            "Original Name",
            DeviceStatusEnum.Available
        );

        string newName = "Updated Name";
        var newStatus = DeviceStatusEnum.Repairing;

        var handler = new UpdateGPSDevice.Handler(_dbContext, _currentUser);
        var command = new UpdateGPSDevice.Command(gpsDevice.Id, newName, newStatus);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Cập nhật thành công", result.SuccessMessage);
        Assert.Equal(gpsDevice.Id, result.Value.Id);

        // Verify device was updated in database
        var updatedDevice = await _dbContext.GPSDevices.FindAsync(gpsDevice.Id);
        Assert.NotNull(updatedDevice);
        Assert.Equal(newName, updatedDevice.Name);
        Assert.Equal(newStatus, updatedDevice.Status);
        Assert.NotNull(updatedDevice.UpdatedAt);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Technician")]
    [InlineData("Driver")]
    [InlineData("Consultant")]
    public async Task Handle_NonAdminUser_ReturnsForbidden(string roleName)
    {
        // Arrange
        var role = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, role);
        _currentUser.SetUser(user);

        var gpsDevice = await TestDataGPSDevice.CreateTestGPSDevice(_dbContext);

        var handler = new UpdateGPSDevice.Handler(_dbContext, _currentUser);
        var command = new UpdateGPSDevice.Command(
            gpsDevice.Id,
            "Updated Name",
            DeviceStatusEnum.Available
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);

        // Verify device was not updated
        var unchangedDevice = await _dbContext.GPSDevices.FindAsync(gpsDevice.Id);
        Assert.Equal("Test GPS Device", unchangedDevice.Name);
        Assert.Equal(DeviceStatusEnum.Available, unchangedDevice.Status);
    }

    [Fact]
    public async Task Handle_DeviceNotFound_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        var nonExistentDeviceId = Guid.NewGuid();

        var handler = new UpdateGPSDevice.Handler(_dbContext, _currentUser);
        var command = new UpdateGPSDevice.Command(
            nonExistentDeviceId,
            "Updated Name",
            DeviceStatusEnum.Broken
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.GPSDeviceNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_DeletedDevice_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        // Create a deleted GPS device
        var deletedDevice = await TestDataGPSDevice.CreateTestGPSDevice(
            _dbContext,
            "Deleted Device",
            DeviceStatusEnum.Available,
            isDeleted: true
        );

        var handler = new UpdateGPSDevice.Handler(_dbContext, _currentUser);
        var command = new UpdateGPSDevice.Command(
            deletedDevice.Id,
            "Updated Name",
            DeviceStatusEnum.Repairing
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.GPSDeviceNotFound, result.Errors);

        // Verify deleted device remains deleted
        var stillDeletedDevice = await _dbContext.GPSDevices.FindAsync(deletedDevice.Id);
        Assert.True(stillDeletedDevice.IsDeleted);
    }

    [Fact]
    public async Task Handle_UpdatesAllFields_Correctly()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        var gpsDevice = await TestDataGPSDevice.CreateTestGPSDevice(
            _dbContext,
            "Original Name",
            DeviceStatusEnum.Available
        );

        // Wait a moment to ensure updated timestamp will be different
        await Task.Delay(10);
        var beforeUpdateTimestamp = gpsDevice.UpdatedAt;

        string newName = "Updated Name";
        var newStatus = DeviceStatusEnum.InUsed;

        var handler = new UpdateGPSDevice.Handler(_dbContext, _currentUser);
        var command = new UpdateGPSDevice.Command(gpsDevice.Id, newName, newStatus);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        var updatedDevice = await _dbContext.GPSDevices.FindAsync(gpsDevice.Id);
        Assert.Equal(newName, updatedDevice!.Name);
        Assert.Equal(newStatus, updatedDevice.Status);

        // Verify timestamp was updated
        Assert.NotEqual(beforeUpdateTimestamp, updatedDevice.UpdatedAt);

        // Verify OSBuildId wasn't changed
        Assert.Equal(gpsDevice.OSBuildId, updatedDevice.OSBuildId);
    }
}
