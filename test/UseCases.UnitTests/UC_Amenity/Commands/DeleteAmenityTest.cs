using Ardalis.Result;

using Domain.Constants;
using Domain.Entities;

using Microsoft.EntityFrameworkCore;

using Persistance.Data;

using UseCases.DTOs;
using UseCases.UC_Amenity.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

using UUIDNext;

namespace UseCases.UnitTests.UC_Amenity.Commands;

[Collection("Test Collection")]
public class DeleteAmenityTests(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new DeleteAmenity.Handler(_dbContext, _currentUser);
        var command = new DeleteAmenity.Command(Uuid.NewDatabaseFriendly(Database.PostgreSql));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_AmenityNotFound_ReturnsError()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new DeleteAmenity.Handler(_dbContext, _currentUser);
        var command = new DeleteAmenity.Command(Uuid.NewDatabaseFriendly(Database.PostgreSql));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.AmenitiesNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_DeletesAmenitySuccessfully()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
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
        Assert.Contains(ResponseMessages.Deleted, result.SuccessMessage);

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
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
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
        Assert.Contains(ResponseMessages.AmenitiesNotFound, result.Errors);
    }
}
