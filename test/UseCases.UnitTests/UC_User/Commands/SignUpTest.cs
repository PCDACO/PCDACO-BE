using Ardalis.Result;

using Domain.Entities;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_User.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;

using UUIDNext;

namespace UseCases.UnitTests.UC_User.Commands;

[Collection("Test Collection")]
public class SignUpTest : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly Func<Task> _resetDatabase;

    private readonly EncryptionSettings _encryptionSettings;
    private readonly IAesEncryptionService _aesService;
    private readonly IKeyManagementService _keyService;
    private readonly TokenService _tokenService;

    public SignUpTest(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _currentUser = fixture.CurrentUser;
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

    [Fact]
    public async Task Handle_ValidRequest_CreatesDriverUserSuccessfully()
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
        Assert.Equal("Driver", createdUser.Role.Name);
        Assert.Equal("newuser@example.com", createdUser.Email);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesOwnerUserSuccessfully()
    {
        // Arrange
        await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");

        var handler = new SignUp.Handler(
            _dbContext,
            _tokenService,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var command = new SignUp.Command(
            "New Owner",
            "newowner@example.com",
            "12345",
            "Hanoi",
            DateTimeOffset.UtcNow.AddYears(-30),
            "0987654999",
            "Owner"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Đăng ký thành công", result.SuccessMessage);

        // Verify database state
        var createdUser = await _dbContext.Users.FirstOrDefaultAsync(u =>
            u.Email == "newowner@example.com"
        );
        Assert.NotNull(createdUser);
        Assert.Equal("Owner", createdUser.Role.Name);
        Assert.Equal("newowner@example.com", createdUser.Email);
    }

    [Fact]
    public async Task Handle_InValidRole_CreatesAdminUserError()
    {
        // Arrange
        await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");

        var handler = new SignUp.Handler(
            _dbContext,
            _tokenService,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var command = new SignUp.Command(
            "New Admin User",
            "newadmin@example.com",
            "password",
            "Hanoi",
            DateTimeOffset.UtcNow.AddYears(-30),
            "0987654321",
            "Admin"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Không thể đăng ký tài khoản với vai trò này", result.Errors.First());
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ReturnsError()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var (existingUser, originalPhone) = await CreateTestUserWithEncryptedPhone(
            userRole,
            "test@example.com",
            "0987654321"
        );
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
            existingUser.Email, // Using the same email as existing user
            "password123",
            existingUser.Address,
            DateTimeOffset.UtcNow.AddYears(-30),
            originalPhone
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Email đã tồn tại", result.Errors.First());
    }

    [Fact]
    public async Task Handle_PhoneAlreadyExists_ReturnsError()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var (existingUser, originalPhone) = await CreateTestUserWithEncryptedPhone(
            userRole,
            "test@example.com",
            "0987654321"
        );
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
            originalPhone // Using the same phone number as existing user
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

    // Helper method to create a user with properly encrypted phone number
    private async Task<(User User, string OriginalPhone)> CreateTestUserWithEncryptedPhone(
        UserRole role,
        string email = "test@example.com",
        string originalPhone = "0987654321"
    )
    {
        // Generate encryption key and IV
        (string key, string iv) = await _keyService.GenerateKeyAsync();

        // Encrypt phone number
        string encryptedPhone = await _aesService.Encrypt(originalPhone, key, iv);

        // Encrypt key with master key
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        // Create encryption key record
        EncryptionKey encryptionKey = new() { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create user with encrypted phone
        User user = new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptionKeyId = encryptionKey.Id,
            Name = "Test User",
            Email = email,
            Password = "password".HashString(),
            RoleId = role.Id,
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Phone = encryptedPhone,
        };

        // Create user statistics
        UserStatistic userStatistic = new() { UserId = user.Id };

        // Save to database
        await _dbContext.Users.AddAsync(user);
        await _dbContext.UserStatistics.AddAsync(userStatistic);
        await _dbContext.SaveChangesAsync();

        return (user, originalPhone);
    }
}
