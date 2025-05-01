using Ardalis.Result;
using Domain.Constants;
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
public class CreateStaffTest : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly Func<Task> _resetDatabase;
    private readonly EncryptionSettings _encryptionSettings;
    private readonly IAesEncryptionService _aesService;
    private readonly IKeyManagementService _keyService;

    public CreateStaffTest(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _currentUser = fixture.CurrentUser;
        _resetDatabase = fixture.ResetDatabaseAsync;
        _encryptionSettings = new EncryptionSettings { Key = TestConstants.MasterKey };
        _aesService = new AesEncryptionService();
        _keyService = new KeyManagementService();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await CreateUserWithEncryptedPhone(
            driverRole,
            "driver@test.com",
            "Test Driver",
            "1234567890"
        );
        _currentUser.SetUser(user);

        var handler = new CreateStaff.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = new CreateStaff.Command(
            "Test Staff",
            "staff@test.com",
            "password123",
            "Test Address",
            DateTimeOffset.UtcNow.AddYears(-25),
            "1234567890",
            "Consultant"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Theory]
    [InlineData("Consultant", "consultant@test.com", "Test Consultant")]
    [InlineData("Technician", "technician@test.com", "Test Technician")]
    public async Task Handle_ValidStaffRequest_CreatesUserSuccessfully(
        string roleName,
        string email,
        string name
    )
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var staffRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var admin = await CreateUserWithEncryptedPhone(
            adminRole,
            "admin@test.com",
            "Admin User",
            "9876543210"
        );
        _currentUser.SetUser(admin);

        var handler = new CreateStaff.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = new CreateStaff.Command(
            name,
            email,
            "password123",
            "Test Address",
            DateTimeOffset.UtcNow.AddYears(-25),
            "1234567997",
            roleName
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotEqual(Guid.Empty, result.Value.Id);

        var createdUser = await _dbContext
            .Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == result.Value.Id);

        Assert.NotNull(createdUser);
        Assert.Equal(roleName, createdUser.Role.Name);
        Assert.Equal(email, createdUser.Email);
    }

    [Fact]
    public async Task Handle_ExistingEmail_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var admin = await CreateUserWithEncryptedPhone(
            adminRole,
            "admin@test.com",
            "Admin User",
            "9876543211"
        );
        var existingUser = await CreateUserWithEncryptedPhone(
            consultantRole,
            "existing@test.com",
            "Existing User",
            "5555555555"
        );
        _currentUser.SetUser(admin);

        var handler = new CreateStaff.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = new CreateStaff.Command(
            "Test User",
            "existing@test.com", // Using existing email
            "password123",
            "Test Address",
            DateTimeOffset.UtcNow.AddYears(-25),
            "9876543210",
            "Consultant"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.EmailAddressIsExisted, result.Errors);
    }

    [Theory]
    [InlineData("", "test@email.com", "password123", "Address", "1234567890", "Consultant")] // Empty name
    [InlineData("Test Name", "invalid-email", "password123", "Address", "1234567890", "Consultant")] // Invalid email
    [InlineData("Test Name", "test@email.com", "12345", "Address", "1234567890", "Consultant")] // Short password
    [InlineData("Test Name", "test@email.com", "password123", "", "1234567890", "Consultant")] // Empty address
    [InlineData("Test Name", "test@email.com", "password123", "Address", "", "Consultant")] // Empty phone
    [InlineData("Test Name", "test@email.com", "password123", "Address", "1234567890", "Invalid")] // Invalid role
    public void Validator_InvalidRequests_ReturnsValidationErrors(
        string name,
        string email,
        string password,
        string address,
        string phone,
        string roleName
    )
    {
        // Arrange
        var validator = new CreateStaff.Validator();
        var command = new CreateStaff.Command(
            name,
            email,
            password,
            address,
            DateTimeOffset.UtcNow.AddYears(-25),
            phone,
            roleName
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
    }

    private async Task<User> CreateUserWithEncryptedPhone(
        UserRole userRole,
        string email = "test@example.com",
        string name = "Test User",
        string phoneNumber = "1234567890",
        string avatarUrl = "http://example.com/avatar.jpg"
    )
    {
        // Generate encryption key and encrypt phone number
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedPhone = await _aesService.Encrypt(phoneNumber, key, iv);
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        // Create encryption key record
        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create user with encrypted phone
        var user = new User
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptionKeyId = encryptionKey.Id,
            Name = name,
            Email = email,
            AvatarUrl = avatarUrl,
            Password = "password".HashString(),
            RoleId = userRole.Id,
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Phone = encryptedPhone,
        };

        // Create user statistics
        var userStatistic = new UserStatistic { UserId = user.Id };

        // Save to database
        await _dbContext.Users.AddAsync(user);
        await _dbContext.UserStatistics.AddAsync(userStatistic);
        await _dbContext.SaveChangesAsync();

        return user;
    }
}
