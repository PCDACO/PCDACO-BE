using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using Persistance.Data;
using UseCases.UC_User.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;
using UUIDNext;

namespace UseCases.UnitTests.UC_User.Commands;

[Collection("Test Collection")]
public class AdminLoginTest : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly Func<Task> _resetDatabase;
    private readonly TokenService _tokenService;

    public AdminLoginTest(DatabaseTestBase fixture)
    {
        var jwtSettings = new JwtSettings
        {
            SecretKey = TestConstants.SecretKey,
            Issuer = "test_issuer",
            Audience = "test_audience",
            TokenExpirationInMinutes = 60,
        };

        _tokenService = new TokenService(jwtSettings);
        _dbContext = fixture.DbContext;
        _resetDatabase = fixture.ResetDatabaseAsync;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

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
        var handler = new AdminLogin.Handler(_dbContext, _tokenService);
        var command = new AdminLogin.Command(testUser.Email, "wrongpassword");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new AdminLogin.Handler(_dbContext, _tokenService);
        var command = new AdminLogin.Command("nonexistent@example.com", "password");

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
        var handler = new AdminLogin.Handler(_dbContext, _tokenService);
        var command = new AdminLogin.Command(testUser.Email, "password");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
    }
}
