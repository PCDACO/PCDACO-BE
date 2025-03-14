using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using Infrastructure.Encryption;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_License.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_License.Queries;

[Collection("Test Collection")]
public class GetLicenseByIdTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;
    private readonly IAesEncryptionService _aesService = fixture.AesEncryptionService;
    private readonly IKeyManagementService _keyService = fixture.KeyManagementService;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Theory]
    [InlineData("Technician")]
    [InlineData("Consultant")]
    public async Task Handle_UserNotDriverOrAdminOrOwner_ReturnsForbidden(string roleName)
    {
        // Arrange
        var role = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, role);
        _currentUser.SetUser(testUser);

        var handler = new GetLicenseById.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetLicenseById.Query(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

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

        var handler = new GetLicenseById.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetLicenseById.Query(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy giấy phép lái xe", result.Errors);
    }

    [Fact]
    public async Task Handle_NonOwnerNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var licenseOwner = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        var otherDriver = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "other@test.com"
        );
        _currentUser.SetUser(otherDriver);

        // Create license for owner
        var license = await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            licenseOwner.Id,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var handler = new GetLicenseById.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetLicenseById.Query(license.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền xem giấy phép này", result.Errors);
    }

    [Theory]
    [InlineData("Driver")] // Test for driver
    [InlineData("Admin")] // Test for admin
    [InlineData("Owner")] // Test for owner
    public async Task Handle_ValidRequest_ReturnsLicenseSuccessfully(string role)
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, role);
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        User differentUser;
        if (role == "Admin")
        {
            var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
            differentUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        }
        else
        {
            differentUser = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        }
        _currentUser.SetUser(testUser);

        // Generate encryption key and encrypted license number
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);
        string licenseNumber = "123456789012";
        string encryptedLicenseNumber = await _aesService.Encrypt(licenseNumber, key, iv);

        // Create encryption key
        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };
        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        // Create license
        var license = new License
        {
            UserId = role == "Admin" ? differentUser.Id : testUser.Id, // Different owner if admin
            EncryptionKeyId = encryptionKey.Id,
            EncryptedLicenseNumber = encryptedLicenseNumber,
            ExpiryDate = DateTimeOffset.UtcNow.AddYears(1),
            LicenseImageFrontUrl = "front-url",
            LicenseImageBackUrl = "back-url",
        };
        await _dbContext.Licenses.AddAsync(license);
        await _dbContext.SaveChangesAsync();

        var handler = new GetLicenseById.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetLicenseById.Query(license.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Lấy thông tin giấy phép lái xe thành công", result.SuccessMessage);

        var response = result.Value;
        Assert.Equal(license.Id, response.Id);
        Assert.Equal(licenseNumber, response.LicenseNumber);
        Assert.Equal(license.ExpiryDate.Date, response.ExpirationDate.Date);
        Assert.Equal(license.LicenseImageFrontUrl, response.ImageFrontUrl);
        Assert.Equal(license.LicenseImageBackUrl, response.ImageBackUrl);
    }
}
