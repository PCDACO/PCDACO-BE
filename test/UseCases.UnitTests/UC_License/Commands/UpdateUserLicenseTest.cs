using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using Infrastructure.Encryption;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_License.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_License.Commands;

[Collection("Test Collection")]
public class UpdateUserLicenseTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly EncryptionSettings _encryptionSettings = new()
    {
        Key = TestConstants.MasterKey,
    };
    private readonly IAesEncryptionService _aesService = new AesEncryptionService();
    private readonly IKeyManagementService _keyService = new KeyManagementService();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    private UpdateUserLicense.Command CreateValidCommand() =>
        new(LicenseNumber: "123456789012", ExpirationDate: DateTime.UtcNow.AddYears(1));

    [Fact]
    public async Task Handle_UserNotDriver_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new UpdateUserLicense.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này", result.Errors);
    }

    [Fact]
    public async Task Handle_LicenseNotFound_ReturnsNotFound()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new UpdateUserLicense.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy giấy phép lái xe", result.Errors);
    }

    [Theory]
    [InlineData("Driver")]
    [InlineData("Owner")]
    public async Task Handle_ValidRequest_UpdatesLicenseSuccessfully(string roleName)
    {
        // Arrange
        var role = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, role);
        _currentUser.SetUser(user);

        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string oldEncryptedLicenseNumber = await _aesService.Encrypt(
            "NTjmhIE3YJtqsqXCZYbjzA==",
            key,
            iv
        );
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        EncryptionKey oldEncryptionKey = new() { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(oldEncryptionKey);
        await _dbContext.SaveChangesAsync();

        var updateUser = await _dbContext.Users.FindAsync(user.Id);
        updateUser!.EncryptionKeyId = oldEncryptionKey.Id;
        updateUser.EncryptedLicenseNumber = oldEncryptedLicenseNumber;
        updateUser.LicenseExpiryDate = DateTimeOffset.UtcNow.AddDays(1);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateUserLicense.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Cập nhật giấy phép lái xe thành công", result.SuccessMessage);

        var userWithLicenseAdded = await _dbContext.Users.FindAsync(user.Id);
        Assert.NotNull(userWithLicenseAdded);
        Assert.NotEmpty(userWithLicenseAdded.EncryptedLicenseNumber);
        Assert.NotEqual(oldEncryptedLicenseNumber, userWithLicenseAdded.EncryptedLicenseNumber);
        Assert.NotEqual(oldEncryptionKey.Id, userWithLicenseAdded.EncryptionKeyId);
        Assert.Equal(
            command.ExpirationDate.Date,
            userWithLicenseAdded.LicenseExpiryDate!.Value.Date
        );
    }

    [Fact]
    public void Validator_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new UpdateUserLicense.Validator();
        var command = new UpdateUserLicense.Command(
            LicenseNumber: "123", // Too short
            ExpirationDate: DateTime.UtcNow.Date.AddDays(-1) // Past date
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LicenseNumber");
        Assert.Contains(result.Errors, e => e.PropertyName == "ExpirationDate");
    }
}
