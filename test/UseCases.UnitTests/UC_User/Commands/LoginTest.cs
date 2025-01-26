using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using UseCases.UC_User.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;
using UUIDNext;

namespace UseCases.UnitTests.UC_User.Commands;

public class LoginTest : DatabaseTestBase
{
    private readonly TokenService _tokenService;

    public LoginTest()
    {
        var jwtSettings = new JwtSettings
        {
            SecretKey = "your_secret_key_for_testing_purposes_only",
            Issuer = "test_issuer",
            Audience = "test_audience",
            TokenExpirationInMinutes = 60,
        };
        _tokenService = new TokenService(jwtSettings);
    }

    private async Task<User> CreateTestUser()
    {
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(_dbContext);

        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var user = new User
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            EncryptionKeyId = encryptionKey.Id,
            Name = "Test User",
            Email = "test@example.com",
            Password = "password".HashString(),
            RoleId = userRole.Id,
            Address = "Test Address",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Phone = "1234567890",
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    [Fact]
    public async Task Handle_InvalidPassword_ReturnsNotFound()
    {
        // Arrange
        var testUser = await CreateTestUser();
        var handler = new Login.Handler(_dbContext, _tokenService);
        var command = new Login.Command(testUser.Email, "wrongpassword");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new Login.Handler(_dbContext, _tokenService);
        var command = new Login.Command("nonexistent@example.com", "password");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var testUser = await CreateTestUser();
        var handler = new Login.Handler(_dbContext, _tokenService);
        var command = new Login.Command(testUser.Email, "password");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
    }
}
