using Ardalis.Result;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.UC_Amenity.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UUIDNext;

namespace UseCases.UnitTests.UC_Amenity.Commands;

public class DeleteAmenityTests : DatabaseTestBase
{
    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Driver);
        _currentUser.SetUser(testUser);

        var handler = new DeleteAmenity.Handler(_dbContext, _currentUser);
        var command = new DeleteAmenity.Command(Uuid.NewDatabaseFriendly(Database.PostgreSql));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền xóa tiện nghi", result.Errors);
    }

    [Fact]
    public async Task Handle_AmenityNotFound_ReturnsError()
    {
        // Arrange
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Admin);
        _currentUser.SetUser(testUser);

        var handler = new DeleteAmenity.Handler(_dbContext, _currentUser);
        var command = new DeleteAmenity.Command(Uuid.NewDatabaseFriendly(Database.PostgreSql));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Không tìm thấy tiện nghi", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_DeletesAmenitySuccessfully()
    {
        // Arrange
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Admin);
        _currentUser.SetUser(testUser);
        var testAmenity = await TestDataCreateAmenity.CreateTestAmenity(
            _dbContext,
            isDeleted: false
        );

        var handler = new DeleteAmenity.Handler(_dbContext, _currentUser);
        var command = new DeleteAmenity.Command(testAmenity.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Contains("Xóa tiện nghi thành công", result.SuccessMessage);

        // Verify soft delete
        var deletedAmenity = await _dbContext
            .Amenities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == testAmenity.Id);

        Assert.True(deletedAmenity?.IsDeleted);
    }

    [Fact]
    public async Task Handle_AmenityAlreadyDeleted_ReturnsError()
    {
        // Arrange
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Admin);
        _currentUser.SetUser(testUser);
        var testAmenity = await TestDataCreateAmenity.CreateTestAmenity(
            _dbContext,
            isDeleted: true
        );

        var handler = new DeleteAmenity.Handler(_dbContext, _currentUser);
        var command = new DeleteAmenity.Command(testAmenity.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Không tìm thấy tiện nghi", result.Errors);
    }
}
