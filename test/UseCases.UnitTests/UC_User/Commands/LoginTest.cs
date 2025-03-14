using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.UC_User.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;

namespace UseCases.UnitTests.UC_User.Commands;

[Collection("Test Collection")]
public class LoginTest : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly Func<Task> _resetDatabase;
    private readonly TokenService _tokenService;
    private readonly IAesEncryptionService _aesService;
    private readonly IKeyManagementService _keyService;
    private readonly EncryptionSettings _encryptionSettings;

    public LoginTest(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _resetDatabase = fixture.ResetDatabaseAsync;

        _encryptionSettings = new EncryptionSettings { Key = TestConstants.MasterKey };
        _aesService = new AesEncryptionService();
        _keyService = new KeyManagementService();

        var jwtSettings = new JwtSettings
        {
            SecretKey = TestConstants.SecretKey,
            Issuer = "test_issuer",
            Audience = "test_audience",
            TokenExpirationInMinutes = 60,
        };
        _tokenService = new TokenService(jwtSettings);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    private async Task<(User User, string DecryptedPhone, string Email)> CreateTestUser(
        string role = "Driver"
    )
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(_dbContext);
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, role);

        string decryptedPhone = "0987654321";
        string key = _keyService.DecryptKey(encryptionKey.EncryptedKey, _encryptionSettings.Key);
        string encryptedPhone = await _aesService.Encrypt(decryptedPhone, key, encryptionKey.IV);

        var user = new User
        {
            EncryptionKeyId = encryptionKey.Id,
            Name = "Test User",
            Email = "test@example.com",
            Password = "password".HashString(),
            RoleId = userRole.Id,
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Phone = encryptedPhone,
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return (user, decryptedPhone, user.Email);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var (_, _, email) = await CreateTestUser();
        var handler = new Login.Handler(_dbContext, _tokenService);
        var command = new Login.Command(email, "password");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Đăng nhập thành công", result.SuccessMessage);
        Assert.NotNull(result.Value.AccessToken);
        Assert.NotNull(result.Value.RefreshToken);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsNotFound()
    {
        // Arrange
        var (_, _, email) = await CreateTestUser();
        var handler = new Login.Handler(_dbContext, _tokenService);
        var command = new Login.Command(email, "wrongpassword");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Sai mật khẩu", result.Errors);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new Login.Handler(_dbContext, _tokenService);
        var command = new Login.Command("nonexistent", "password");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy thông tin người dùng", result.Errors);
    }

    [Fact]
    public async Task Handle_DeletedUser_ReturnsNotFound()
    {
        // Arrange
        var (user, _, email) = await CreateTestUser();
        user.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        var handler = new Login.Handler(_dbContext, _tokenService);
        var command = new Login.Command(email, "password");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy thông tin người dùng", result.Errors);
    }
}
