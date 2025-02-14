using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using Infrastructure.Encryption;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Driver.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Driver.Commands;

[Collection("Test Collection")]
public class UpdateDriverLicenseTest(DatabaseTestBase fixture) : IAsyncLifetime
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

    private UpdateDriverLicense.Command CreateValidCommand(Guid licenseId) =>
        new(
            LicenseId: licenseId,
            LicenseNumber: "123456789012",
            ExpirationDate: DateTime.UtcNow.AddYears(1)
        );

    [Fact]
    public async Task Handle_UserNotDriver_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new UpdateDriverLicense.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = CreateValidCommand(Guid.NewGuid());

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

        var handler = new UpdateDriverLicense.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = CreateValidCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy giấy phép lái xe", result.Errors);
    }

    [Fact]
    public async Task Handle_NotOwnerOfLicense_ReturnsForbidden()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var owner = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var otherDriver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "other@test.com"
        );
        _currentUser.SetUser(otherDriver);

        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(_dbContext);
        var license = new License
        {
            UserId = owner.Id,
            EncryptionKeyId = encryptionKey.Id,
            ExpiryDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
        };
        await _dbContext.Licenses.AddAsync(license);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateDriverLicense.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = CreateValidCommand(license.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesLicenseSuccessfully()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(driver);

        var oldEncryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(
            _dbContext
        );
        var oldEncryptedLicenseNumber = "NTjmhIE3YJtqsqXCZYbjzA==";
        var license = new License
        {
            UserId = driver.Id,
            EncryptionKeyId = oldEncryptionKey.Id,
            EncryptedLicenseNumber = oldEncryptedLicenseNumber,
            ExpiryDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
        };
        await _dbContext.Licenses.AddAsync(license);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateDriverLicense.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = CreateValidCommand(license.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Cập nhật giấy phép lái xe thành công", result.SuccessMessage);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        Assert.NotNull(updatedLicense);
        Assert.NotEqual(oldEncryptedLicenseNumber, updatedLicense.EncryptedLicenseNumber);
        Assert.NotEqual(oldEncryptionKey.Id, updatedLicense.EncryptionKeyId);
        Assert.Equal(command.ExpirationDate.ToString("yyyy-MM-dd"), updatedLicense.ExpiryDate);
    }

    [Fact]
    public void Validator_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new UpdateDriverLicense.Validator();
        var command = new UpdateDriverLicense.Command(
            LicenseId: Guid.Empty,
            LicenseNumber: "123", // Too short
            ExpirationDate: DateTime.UtcNow.Date.AddDays(-1) // Past date
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LicenseId");
        Assert.Contains(result.Errors, e => e.PropertyName == "LicenseNumber");
        Assert.Contains(result.Errors, e => e.PropertyName == "ExpirationDate");
    }
}
