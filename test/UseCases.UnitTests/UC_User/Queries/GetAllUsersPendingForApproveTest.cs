using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_User.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_User.Queries;

[Collection("Test Collection")]
public class GetAllUsersPendingForApproveTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly AesEncryptionService _encryptionService = fixture.AesEncryptionService;
    private readonly KeyManagementService _keyManagementService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_WithKeyword_ReturnsFilteredUsers()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(admin);

        // Create test users with different names/emails
        var user1 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "match@test.com",
            "Match User",
            "1234567890"
        );
        var user2 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "other@test.com",
            "Other User",
            "0987654321"
        );

        // Create licenses for both users
        await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            user1.Id,
            _encryptionService,
            _keyManagementService,
            _encryptionSettings
        );
        await EncryptPhone(user1);
        await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            user2.Id,
            _encryptionService,
            _keyManagementService,
            _encryptionSettings
        );
        await EncryptPhone(user2);

        var handler = new GetAllUsersPendingForApprove.Handler(
            _dbContext,
            _currentUser,
            _encryptionService,
            _keyManagementService,
            _encryptionSettings
        );

        var query = new GetAllUsersPendingForApprove.Query(1, 10, "maTc");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal("Match User", result.Value.Items.First().Name);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsPaginatedLicenses()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(admin);

        // Create multiple test users
        var users = await TestDataCreateUser.CreateTestUserList(_dbContext, driverRole);

        // Create licenses for each user
        foreach (var user in users)
        {
            await TestDataCreateLicense.CreateTestLicense(
                _dbContext,
                user.Id,
                _encryptionService,
                _keyManagementService,
                _encryptionSettings
            );
            await EncryptPhone(user);
        }

        var handler = new GetAllUsersPendingForApprove.Handler(
            _dbContext,
            _currentUser,
            _encryptionService,
            _keyManagementService,
            _encryptionSettings
        );

        var query = new GetAllUsersPendingForApprove.Query(1, 2, ""); // Request first page with 2 items

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.Items.Count());
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(1, result.Value.PageNumber);
        Assert.Equal(2, result.Value.PageSize);
    }

    // Helper method to encrypt phone number by user's encryption key
    private async Task<string> EncryptPhone(User user)
    {
        string key = _keyManagementService.DecryptKey(
            user.EncryptionKey.EncryptedKey,
            _encryptionSettings.Key
        );

        var encryptedPhone = await _encryptionService.Encrypt(
            user.Phone,
            key,
            user.EncryptionKey.IV
        );

        var updateUser = await _dbContext.Users.FindAsync(user.Id);
        updateUser!.Phone = encryptedPhone;
        await _dbContext.SaveChangesAsync();

        return encryptedPhone;
    }
}
