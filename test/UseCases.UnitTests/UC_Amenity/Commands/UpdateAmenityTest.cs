using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.UC_Amenity.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Amenity.Commands;

public class UpdateAmenityTests : DatabaseTestBase
{
    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new UpdateAmenity.Handler(_dbContext, _currentUser);
        var command = new UpdateAmenity.Command(
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            "New Name",
            "New Description"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện thao tác này", result.Errors);
    }

    [Fact]
    public async Task Handle_AmenityNotFound_ReturnsNotFound()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new UpdateAmenity.Handler(_dbContext, _currentUser);
        var command = new UpdateAmenity.Command(
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            "New Name",
            "New Description"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy tiện nghi", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesAmenitySuccessfully()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);
        var testAmenity = await TestDataCreateAmenity.CreateTestAmenity(
            _dbContext,
            isDeleted: false
        );

        var handler = new UpdateAmenity.Handler(_dbContext, _currentUser);
        var command = new UpdateAmenity.Command(
            testAmenity.Id,
            "Updated Name",
            "Updated Description"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Contains("Cập nhật tiện nghi thành công", result.SuccessMessage);

        var updatedAmenity = await _dbContext
            .Amenities.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == testAmenity.Id);

        Assert.NotNull(updatedAmenity);
        Assert.Equal("Updated Name", updatedAmenity.Name);
        Assert.Equal("Updated Description", updatedAmenity.Description);
        Assert.True(DateTimeOffset.UtcNow >= updatedAmenity.UpdatedAt);
    }
}
