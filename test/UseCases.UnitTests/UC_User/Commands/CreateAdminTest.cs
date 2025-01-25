using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using UseCases.Abstractions;
using UseCases.UC_User.Commands;

namespace UseCases.UnitTests.UC_User.Commands;

public class CreateAdminTest
{
    private readonly Mock<IAppDBContext> _mockContext;
    private readonly Mock<IAesEncryptionService> _mockAesEncryptionService;
    private readonly Mock<IKeyManagementService> _mockKeyManagementService;
    private readonly EncryptionSettings _encryptionSettings;

    public CreateAdminTest()
    {
        _mockContext = new Mock<IAppDBContext>();
        _mockAesEncryptionService = new Mock<IAesEncryptionService>();
        _mockKeyManagementService = new Mock<IKeyManagementService>();
        _encryptionSettings = new EncryptionSettings { Key = "encryptionKey" };
    }

    private static User CreateTestUser(UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            EncryptionKeyId = Guid.NewGuid(),
            Name = "Admin",
            Email = "admin@gmail.com",
            Password = "admin",
            Role = role,
            Address = "Hanoi",
            DateOfBirth = DateTime.Now.AddYears(-30),
            Phone = "0123456789",
        };
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
        where T : class
    {
        var mockSet = data.BuildMock().BuildMockDbSet();
        return mockSet;
    }

    [Fact]
    public async Task Handle_AdminAlreadyExists_ReturnsError()
    {
        // Arrange
        var existingAdmin = CreateTestUser(UserRole.Admin);

        var mockUsers = CreateMockDbSet(new List<User> { existingAdmin });
        _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);

        var handler = new CreateAdminUser.Handler(
            _mockContext.Object,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );
        var command = new CreateAdminUser.Command();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Tài khoản đã được khởi tạo !", result.Errors.First());
    }

    [Fact]
    public async Task Handle_CreatesAdminSuccessfully()
    {
        // Arrange
        var mockUsers = CreateMockDbSet(new List<User>());
        var mockEncryptionKeys = CreateMockDbSet(new List<EncryptionKey>());
        _mockContext.Setup(c => c.Users).Returns(mockUsers.Object);
        _mockContext.Setup(c => c.EncryptionKeys).Returns(mockEncryptionKeys.Object);

        _mockKeyManagementService.Setup(k => k.GenerateKeyAsync()).ReturnsAsync(("key", "iv"));
        _mockKeyManagementService
            .Setup(k => k.EncryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("encryptedKey");
        _mockAesEncryptionService
            .Setup(a => a.Encrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("encryptedPhoneNumber");

        _mockContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateAdminUser.Handler(
            _mockContext.Object,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );
        var command = new CreateAdminUser.Command();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Tạo tài khoản admin thành công", result.SuccessMessage);
        mockUsers.Verify(
            m => m.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        mockEncryptionKeys.Verify(
            m => m.AddAsync(It.IsAny<EncryptionKey>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
