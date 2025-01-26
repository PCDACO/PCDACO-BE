using Ardalis.Result;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.UC_User.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_User.Commands;

public class CreateAdminTest : DatabaseTestBase
{
    private readonly EncryptionSettings _encryptionSettings;
    private readonly IAesEncryptionService _aesService;
    private readonly IKeyManagementService _keyService;

    public CreateAdminTest()
    {
        _encryptionSettings = new EncryptionSettings { Key = TestConstants.MasterKey };
        _aesService = new AesEncryptionService();
        _keyService = new KeyManagementService();
    }

    [Fact]
    public async Task Handle_AdminAlreadyExists_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var existingAdmin = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            adminRole,
            "admin@gmail.com"
        );
        _currentUser.SetUser(existingAdmin);

        var handler = new CreateAdminUser.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var command = new CreateAdminUser.Command();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("Tài khoản đã được khởi tạo !", result.Errors.First());
    }

    [Fact(Timeout = 3000)]
    public async Task Handle_CreatesAdminSuccessfully()
    {
        // Arrange
        await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");

        var handler = new CreateAdminUser.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var command = new CreateAdminUser.Command();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Tạo tài khoản admin thành công", result.SuccessMessage);

        // Verify database state
        var createdAdmin = await _dbContext.Users.FirstOrDefaultAsync(u =>
            u.Email == "admin@gmail.com"
        );
        Assert.NotNull(createdAdmin);
        Assert.Equal("admin@gmail.com", createdAdmin.Email);
    }

    [Fact]
    public async Task Handle_AdminRoleNotFound_ReturnsError()
    {
        // Arrange
        var handler = new CreateAdminUser.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var command = new CreateAdminUser.Command();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Equal("Không thể tạo tài khoản admin", result.Errors.First());
    }
}
