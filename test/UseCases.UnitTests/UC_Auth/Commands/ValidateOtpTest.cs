using System.IdentityModel.Tokens.Jwt;
using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Auth.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;

namespace UseCases.UnitTests.UC_Auth.Commands;

[Collection("Test Collection")]
public class ValidateOtpTest : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly TokenService _tokenService;
    private readonly Func<Task> _resetDatabase;

    public ValidateOtpTest(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _resetDatabase = fixture.ResetDatabaseAsync;

        // Initialize OTP service with a memory cache
        _otpService = new OtpService(new MemoryCache(new MemoryCacheOptions()));

        // Initialize token service with test settings
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

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new ValidateOtp.Handler(_dbContext, _otpService, _tokenService);

        var command = new ValidateOtp.Command("nonexistent@example.com", "123456");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy người dùng với email này", result.Errors);
    }

    [Fact]
    public async Task Handle_DeletedUser_ReturnsNotFound()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            userRole,
            "deleted@example.com",
            "Deleted User",
            "1234567890"
        );

        // Store OTP for this user
        string otp = "123456";
        _otpService.StoreOtp(testUser.Email, otp);

        // Mark user as deleted
        testUser.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        var handler = new ValidateOtp.Handler(_dbContext, _otpService, _tokenService);

        var command = new ValidateOtp.Command(testUser.Email, otp);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy người dùng với email này", result.Errors);
    }

    [Fact]
    public async Task Handle_InvalidOtp_ReturnsError()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            userRole,
            "test@example.com",
            "Test User",
            "1234567890"
        );

        // Store OTP for this user
        _otpService.StoreOtp(testUser.Email, "123456");

        var handler = new ValidateOtp.Handler(_dbContext, _otpService, _tokenService);

        // Use incorrect OTP
        var command = new ValidateOtp.Command(testUser.Email, "654321");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Mã OTP không hợp lệ hoặc đã hết hạn", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidOtp_ReturnsTokens()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            userRole,
            "valid@example.com",
            "Valid User",
            "1234567890"
        );

        // Store OTP for this user
        string otp = "123456";
        _otpService.StoreOtp(testUser.Email, otp);

        var handler = new ValidateOtp.Handler(_dbContext, _otpService, _tokenService);

        var command = new ValidateOtp.Command(testUser.Email, otp);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Xác thực OTP thành công", result.SuccessMessage);

        // Verify tokens were returned
        Assert.NotNull(result.Value.AccessToken);
        Assert.NotNull(result.Value.RefreshToken);

        // Verify OTP is no longer valid (it should be consumed)
        var invalidationResult = await handler.Handle(command, CancellationToken.None);
        Assert.Equal(ResultStatus.Error, invalidationResult.Status);

        // Verify refresh token is stored in database
        var storedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt =>
            rt.UserId == testUser.Id && rt.IsRevoked == false && rt.IsUsed == true
        );
        Assert.NotNull(storedToken);
        Assert.Equal(result.Value.RefreshToken, storedToken.Token);

        // Verify token contains expected claims
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(result.Value.AccessToken);
        Assert.Contains(
            jwtToken.Claims,
            c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == testUser.Id.ToString()
        );
    }

    [Fact]
    public async Task Handle_ValidOtp_InvalidatesPreviousRefreshTokens()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            userRole,
            "revoketest@example.com",
            "Revoke Test User",
            "1234567890"
        );

        // Add existing refresh tokens for this user
        var existingToken = new RefreshToken
        {
            Token = "existing-token",
            UserId = testUser.Id,
            ExpiryDate = DateTimeOffset.UtcNow.AddHours(1),
        };

        await _dbContext.RefreshTokens.AddAsync(existingToken);
        await _dbContext.SaveChangesAsync();

        // Store OTP for this user
        string otp = "123456";
        _otpService.StoreOtp(testUser.Email, otp);

        var handler = new ValidateOtp.Handler(_dbContext, _otpService, _tokenService);

        var command = new ValidateOtp.Command(testUser.Email, otp);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify new token exists
        var newToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt =>
            rt.Token == result.Value.RefreshToken
        );
        Assert.NotNull(newToken);
        Assert.True(newToken.IsUsed);
        Assert.False(newToken.IsRevoked);
    }

    [Theory]
    [InlineData("", "123456", "Email không được để trống")]
    [InlineData("invalid-email", "123456", "Email không hợp lệ")]
    [InlineData("test@example.com", "", "Mã OTP không được để trống")]
    [InlineData("test@example.com", "12345", "Mã OTP phải có 6 ký tự")]
    [InlineData("test@example.com", "12345a", "Mã OTP chỉ chứa số")]
    public void Validator_InvalidInput_ReturnsValidationErrors(
        string email,
        string otp,
        string expectedError
    )
    {
        // Arrange
        var validator = new ValidateOtp.Validator();
        var command = new ValidateOtp.Command(email, otp);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(expectedError, result.Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public void Validator_ValidInput_PassesValidation()
    {
        // Arrange
        var validator = new ValidateOtp.Validator();
        var command = new ValidateOtp.Command("valid@example.com", "123456");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
