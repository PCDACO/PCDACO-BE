using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Reflection;
using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query; // Add this namespace
using MockQueryable.Moq;
using Moq;
using UseCases.Abstractions;
using UseCases.UC_User.Commands;
using UseCases.Utils;

namespace UseCases.UnitTests.UC_User.Commands;

public class LoginTest
{
    private readonly Mock<IAppDBContext> _mockContext;
    private readonly TokenService _tokenService;

    public LoginTest()
    {
        _mockContext = new Mock<IAppDBContext>();

        var jwtSettings = new JwtSettings
        {
            SecretKey = "your_secret_key_for_testing_purposes_only",
            Issuer = "test_issuer",
            Audience = "test_audience",
            TokenExpirationInMinutes = 60,
        };
        _tokenService = new TokenService(jwtSettings);
    }

    private static User CreateTestUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            EncryptionKeyId = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            Password = "password".HashString(),
            Role = UserRole.Driver,
            Address = "Test Address",
            DateOfBirth = DateTime.Now.AddYears(-30),
            Phone = "1234567890",
        };
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
        where T : class
    {
        var mockSet = data.BuildMock().BuildMockDbSet();
        return mockSet;
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockUsers = CreateMockDbSet(new List<User>());
        _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);

        var handler = new Login.Handler(_mockContext.Object, _tokenService);
        var command = new Login.Command("nonexistent@example.com", "password");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_InvalidPassword_ReturnsNotFound()
    {
        // Arrange
        var testUser = CreateTestUser();
        var mockUsers = CreateMockDbSet(new List<User> { testUser });
        _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);

        var handler = new Login.Handler(_mockContext.Object, _tokenService);
        var command = new Login.Command(testUser.Email, "wrongpassword");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_DatabaseError_ReturnsException()
    {
        // Arrange
        var testUser = CreateTestUser();
        var mockUsers = CreateMockDbSet(new List<User> { testUser });
        var mockRefreshTokens = CreateMockDbSet(new List<RefreshToken>());

        _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
        _mockContext.Setup(c => c.RefreshTokens).Returns(mockRefreshTokens.Object);

        _mockContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Test database error"));

        var handler = new Login.Handler(_mockContext.Object, _tokenService);
        var command = new Login.Command(testUser.Email, "password");

        // Act & Assert
        await Assert.ThrowsAsync<TargetInvocationException>(
            () => handler.Handle(command, CancellationToken.None)
        );
    }
}
