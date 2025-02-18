using Ardalis.Result;
using Domain.Constants;
using Domain.Shared;
using Infrastructure.Encryption;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_User.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_User.Commands;

[Collection("Test Collection")]
public class UpdateUserTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly EncryptionSettings _encryptionSettings = new()
    {
        Key = TestConstants.MasterKey,
    };
    private readonly AesEncryptionService _aesService = new AesEncryptionService();
    private readonly KeyManagementService _keyService = new KeyManagementService();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_ValidRequest_UpdatesUserSuccessfully()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);

        var handler = new UpdateUser.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = new UpdateUser.Command(
            user.Id,
            "Updated Name",
            "updated@example.com",
            "Updated Address",
            DateTimeOffset.UtcNow.AddYears(-25),
            "0987654321"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(ResponseMessages.Updated, result.SuccessMessage);

        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(command.Name, updatedUser.Name);
        Assert.Equal(command.Email, updatedUser.Email);
        Assert.Equal(command.Address, updatedUser.Address);
        Assert.Equal(command.DateOfBirth, updatedUser.DateOfBirth);
        Assert.True(DateTimeOffset.UtcNow >= updatedUser.UpdatedAt);

        // Verify phone encryption
        string decryptedKey = _keyService.DecryptKey(
            updatedUser.EncryptionKey.EncryptedKey,
            _encryptionSettings.Key
        );
        string decryptedPhone = await _aesService.Decrypt(
            updatedUser.Phone,
            decryptedKey,
            updatedUser.EncryptionKey.IV
        );
        Assert.Equal(command.Phone, decryptedPhone);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);

        var handler = new UpdateUser.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = new UpdateUser.Command(
            Guid.NewGuid(), // Non-existent user ID
            "Updated Name",
            "updated@example.com",
            "Updated Address",
            DateTimeOffset.UtcNow.AddYears(-25),
            "0987654321"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.UserNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_UnauthorizedAccess_ReturnsForbidden()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var targetUser = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        var differentUser = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            userRole,
            "different@example.com"
        );
        _currentUser.SetUser(differentUser);

        var handler = new UpdateUser.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = new UpdateUser.Command(
            targetUser.Id,
            "Updated Name",
            "updated@example.com",
            "Updated Address",
            DateTimeOffset.UtcNow.AddYears(-25),
            "0987654321"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public void Validator_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new UpdateUser.Validator();
        var command = new UpdateUser.Command(
            Guid.NewGuid(),
            "", // Empty name
            "invalid-email", // Invalid email format
            "", // Empty address
            DateTimeOffset.UtcNow.AddDays(1), // Future date
            "" // Empty phone
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
        Assert.Contains(result.Errors, e => e.PropertyName == "Address");
        Assert.Contains(result.Errors, e => e.PropertyName == "DateOfBirth");
        Assert.Contains(result.Errors, e => e.PropertyName == "Phone");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Tên không được để trống");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Email không hợp lệ");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Địa chỉ không được để trống");
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage == "Ngày sinh phải nhỏ hơn ngày hiện tại"
        );
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Số điện thoại không được để trống");
    }
}
