using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using Infrastructure.Encryption;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.UC_User.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;

namespace UseCases.UnitTests.UC_User.Queries;

[Collection("Test Collection")]
public class GetUserByIdTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly EncryptionSettings _encryptionSettings = new()
    {
        Key = TestConstants.MasterKey,
    };
    private readonly IAesEncryptionService _aesService = new AesEncryptionService();
    private readonly IKeyManagementService _keyService = new KeyManagementService();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new GetUserById.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetUserById.Query(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.UserNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_DeletedUser_ReturnsNotFound()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);

        // Mark user as deleted
        user.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        var handler = new GetUserById.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetUserById.Query(user.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.UserNotFound, result.Errors);
    }

    [Theory]
    [InlineData("Driver")]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("Consultant")]
    [InlineData("Technician")]
    public async Task Handle_ValidRequest_ReturnsUserSuccessfully(string roleName)
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var phone = "0987654321";

        // Create encryption key
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);
        string encryptedPhone = await _aesService.Encrypt(phone, key, iv);

        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create user
        var user = new User
        {
            EncryptionKeyId = encryptionKey.Id,
            Name = "Test User",
            Email = "test@example.com",
            Password = "password".HashString(),
            RoleId = userRole.Id,
            Address = "Test Address",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-30),
            Phone = encryptedPhone,
        };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var handler = new GetUserById.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetUserById.Query(user.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);

        var response = result.Value;
        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Name, response.Name);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.Address, response.Address);
        Assert.Equal(user.DateOfBirth.Date, response.DateOfBirth.Date);
        Assert.Equal(phone, response.Phone); // Verify decrypted phone
        Assert.Equal(roleName, response.Role);
    }
}
