using Ardalis.Result;
using Domain.Constants;
using Domain.Shared;
using Infrastructure.Encryption;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_User.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_User.Queries;

[Collection("Test Collection")]
public class GetUserPendingForApprovalTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly AesEncryptionService _encryptionService = fixture.AesEncryptionService;
    private readonly KeyManagementService _keyManagementService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(driver);

        var handler = new GetUserPendingForApproval.Handler(
            _dbContext,
            _currentUser,
            _encryptionService,
            _keyManagementService,
            _encryptionSettings
        );

        var query = new GetUserPendingForApproval.Query(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền truy cập", result.Errors);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(admin);

        var handler = new GetUserPendingForApproval.Handler(
            _dbContext,
            _currentUser,
            _encryptionService,
            _keyManagementService,
            _encryptionSettings
        );

        var query = new GetUserPendingForApproval.Query(Guid.NewGuid()); // Non-existent ID

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.UserNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_UserWithoutLicense_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);

        _currentUser.SetUser(admin);

        var handler = new GetUserPendingForApproval.Handler(
            _dbContext,
            _currentUser,
            _encryptionService,
            _keyManagementService,
            _encryptionSettings
        );

        var query = new GetUserPendingForApproval.Query(driver.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.UserNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsUserDetails()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        var driver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver@test.com",
            "Test Driver",
            "0987654321"
        );

        _currentUser.SetUser(admin);

        // Create license for the driver
        var user = await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            driver.Id,
            _encryptionService,
            _keyManagementService,
            _encryptionSettings,
            licenseNumber: "123456789"
        );

        // Encrypt the phone
        string key = _keyManagementService.DecryptKey(
            user.EncryptionKey.EncryptedKey,
            _encryptionSettings.Key
        );

        var encryptedPhone = await _encryptionService.Encrypt(
            user.Phone,
            key,
            user.EncryptionKey.IV
        );

        var updateUser = await _dbContext.Users.FindAsync(user.Id);
        updateUser!.Phone = encryptedPhone;
        await _dbContext.SaveChangesAsync();

        var handler = new GetUserPendingForApproval.Handler(
            _dbContext,
            _currentUser,
            _encryptionService,
            _keyManagementService,
            _encryptionSettings
        );

        var query = new GetUserPendingForApproval.Query(driver.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Lấy danh sách người lái xe thành công", result.SuccessMessage);

        var response = result.Value;
        Assert.Equal(driver.Id, response.Id);
        Assert.Equal("Test Driver", response.Name);
        Assert.Equal("driver@test.com", response.Email);
        Assert.Equal("0987654321", response.Phone); // Should be decrypted
        Assert.Equal("Driver", response.Role);
        Assert.Equal("123456789", response.LicenseNumber); // Should be decrypted
        Assert.NotNull(response.LicenseExpiryDate);
        Assert.NotNull(response.LicenseImageFrontUrl);
        Assert.NotNull(response.LicenseImageBackUrl);
        Assert.Null(response.IsApprovedLicense); // Not yet approved
        Assert.Null(response.LicenseRejectReason);
        Assert.Null(response.LicenseApprovedAt);
        Assert.NotNull(response.LicenseImageUploadedAt);
    }

    [Fact]
    public async Task Handle_AlreadyApprovedLicense_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);

        _currentUser.SetUser(admin);

        // Create an already approved license
        var user = await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            driver.Id,
            _encryptionService,
            _keyManagementService,
            _encryptionSettings,
            isApproved: true
        );

        var handler = new GetUserPendingForApproval.Handler(
            _dbContext,
            _currentUser,
            _encryptionService,
            _keyManagementService,
            _encryptionSettings
        );

        var query = new GetUserPendingForApproval.Query(driver.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.UserNotFound, result.Errors);
    }
}
