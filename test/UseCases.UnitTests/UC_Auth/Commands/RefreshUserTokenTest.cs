using Ardalis.Result;

using Domain.Entities;
using Domain.Shared;

using Microsoft.EntityFrameworkCore;

using Persistance.Data;

using UseCases.UC_Auth.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;

namespace UseCases.UnitTests.UC_Auth.Commands;

[Collection("Test Collection")]
public class RefreshTokenTests : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly TokenService _tokenService;
    private readonly Func<Task> _resetDatabase;

    public RefreshTokenTests(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _resetDatabase = fixture.ResetDatabaseAsync;

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

    private async Task<(User User, string RefreshToken)> CreateTestUserWithRefreshToken(
        bool isRevoked = false,
        bool isExpired = false
    )
    {
        // Create test user
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);

        // Create refresh token
        var refreshToken = new RefreshToken
        {
            Token = _tokenService.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiryDate = isExpired
                ? DateTimeOffset.UtcNow.AddMinutes(-10)
                : DateTimeOffset.UtcNow.AddMinutes(60),
            RevokedAt = isRevoked ? DateTimeOffset.UtcNow : null,
        };

        await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();

        return (user, refreshToken.Token);
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var handler = new RefreshUserToken.Handler(_dbContext, _tokenService);
        var command = new RefreshUserToken.Command("invalid-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        Assert.Contains("Token làm mới không hợp lệ", result.Errors);
    }

    [Fact]
    public async Task Handle_RevokedRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var (_, token) = await CreateTestUserWithRefreshToken(isRevoked: true);
        var handler = new RefreshUserToken.Handler(_dbContext, _tokenService);
        var command = new RefreshUserToken.Command(token);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        Assert.Contains("Token đã bị thu hồi", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange
        var (user, token) = await CreateTestUserWithRefreshToken();
        var handler = new RefreshUserToken.Handler(_dbContext, _tokenService);
        var command = new RefreshUserToken.Command(token);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Làm mới token thành công", result.SuccessMessage);
        Assert.NotNull(result.Value.AccessToken);
        Assert.NotNull(result.Value.RefreshToken);
        Assert.NotEqual(token, result.Value.RefreshToken);

        // Verify old token is revoked
        var oldToken = await _dbContext.RefreshTokens.FirstAsync(rt => rt.Token == token);
        Assert.NotNull(oldToken.RevokedAt);

        // Verify new token exists in database
        var newToken = await _dbContext.RefreshTokens.FirstAsync(rt =>
            rt.Token == result.Value.RefreshToken
        );
        Assert.Equal(user.Id, newToken.UserId);
        Assert.Null(newToken.RevokedAt);
        Assert.True(newToken.ExpiryDate > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Validator_EmptyRefreshToken_ReturnsValidationError()
    {
        // Arrange
        var validator = new RefreshUserToken.Validator();
        var command = new RefreshUserToken.Command(string.Empty);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Token làm mới không được để trống");
    }
}
