using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.UC_User.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;
using UUIDNext;

namespace UseCases.UnitTests.UC_User.Commands;

public class SignUpTest : DatabaseTestBase
{
    private readonly EncryptionSettings _encryptionSettings;
    private readonly IAesEncryptionService _aesService;
    private readonly IKeyManagementService _keyService;
    private readonly TokenService _tokenService;

    public SignUpTest()
    {
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

    [Fact]
    public async Task Handle_ValidRequest_CreatesUserSuccessfully()
    {
        // Arrange
        await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var handler = new SignUp.Handler(
            _dbContext,
            _tokenService,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var command = new SignUp.Command(
            "New User",
            "newuser@example.com",
            "password",
            "Hanoi",
            DateTimeOffset.UtcNow.AddYears(-30),
            "0987654321"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Đăng ký thành công", result.SuccessMessage);

        // Verify database state
        var createdUser = await _dbContext.Users.FirstOrDefaultAsync(u =>
            u.Email == "newuser@example.com"
        );
        Assert.NotNull(createdUser);
        Assert.Equal("newuser@example.com", createdUser.Email);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ReturnsError()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var existingUser = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(existingUser);

        var handler = new SignUp.Handler(
            _dbContext,
            _tokenService,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var command = new SignUp.Command(
            existingUser.Name,
            existingUser.Email,
            existingUser.Password,
            existingUser.Address,
            DateTimeOffset.UtcNow.AddYears(-30),
            existingUser.Phone
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Email đã tồn tại", result.Errors.First());
    }

    [Fact(Timeout = 3000)]
    public async Task Handle_PhoneAlreadyExists_ReturnsError()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var existingUser = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(existingUser);

        var handler = new SignUp.Handler(
            _dbContext,
            _tokenService,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var command = new SignUp.Command(
            "New User",
            "newuser@example.com",
            "password",
            "Hanoi",
            DateTimeOffset.UtcNow.AddYears(-30),
            existingUser.Phone
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Số điện thoại đã tồn tại", result.Errors.First());
    }

    [Fact]
    public async Task Handle_InvalidEmailFormat_ReturnsValidationError()
    {
        // Arrange
        await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var validator = new SignUp.Validator();
        var command = new SignUp.Command(
            "New User",
            "invalid-email",
            "password",
            "Hanoi",
            DateTimeOffset.UtcNow.AddYears(-30),
            "0987654321"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Email không hợp lệ");
    }

    [Fact]
    public async Task Validator_WeakPassword_ReturnsValidationError()
    {
        // Arrange
        await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var validator = new SignUp.Validator();
        var command = new SignUp.Command(
            "New User",
            "newuser@example.com",
            "123",
            "Hanoi",
            DateTimeOffset.UtcNow.AddYears(-30),
            "0987654321"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Mật khẩu phải có ít nhất 6 ký tự");
    }
}
