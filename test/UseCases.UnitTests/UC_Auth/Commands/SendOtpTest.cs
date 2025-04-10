using Ardalis.Result;
using Hangfire;
using Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Auth.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Auth.Commands;

[Collection("Test Collection")]
public class SendOtpTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    private readonly TestDataEmailService _emailService = new();
    private readonly IOtpService _otpService = new OtpService(
        new MemoryCache(new MemoryCacheOptions())
    );
    private readonly IBackgroundJobClient _backgroundJobClient = new BackgroundJobClient();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserFound_SendsOtp()
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

        var handler = new SendOtp.Handler(
            _dbContext,
            _emailService,
            _otpService,
            _backgroundJobClient
        );

        var command = new SendOtp.Command("test@example.com", true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Mã OTP đã được gửi đến email của bạn", result.SuccessMessage);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new SendOtp.Handler(
            _dbContext,
            _emailService,
            _otpService,
            _backgroundJobClient
        );

        var command = new SendOtp.Command("nonexistent@example.com", true);

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

        // Mark user as deleted
        testUser.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        var handler = new SendOtp.Handler(
            _dbContext,
            _emailService,
            _otpService,
            _backgroundJobClient
        );

        var command = new SendOtp.Command("deleted@example.com", true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy người dùng với email này", result.Errors);
    }

    [Fact]
    public async Task SendOtpEmail_SendsCorrectEmail()
    {
        // Arrange
        var handler = new SendOtp.Handler(
            _dbContext,
            _emailService,
            _otpService,
            _backgroundJobClient
        );

        string email = "test@example.com";
        string otp = "123456";
        string userName = "Test User";

        // Act
        await handler.SendOtpEmail(email, otp, userName);

        // Assert
        Assert.Single(_emailService.SentEmails);
        var sentEmail = _emailService.SentEmails.First();

        Assert.Equal(email, sentEmail.Receiver);
        Assert.Equal("Mã xác thực (OTP) của bạn", sentEmail.Subject);
        Assert.Contains(otp, sentEmail.HtmlBody);
        Assert.Contains(userName, sentEmail.HtmlBody);
    }

    [Theory]
    [InlineData("", "Email không được để trống")]
    [InlineData("invalid-email", "Email không hợp lệ")]
    public void Validator_InvalidEmail_ReturnsValidationError(string email, string expectedError)
    {
        // Arrange
        var validator = new SendOtp.Validator();
        var command = new SendOtp.Command(email);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(expectedError, result.Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public void Validator_ValidEmail_PassesValidation()
    {
        // Arrange
        var validator = new SendOtp.Validator();
        var command = new SendOtp.Command("valid@example.com");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
