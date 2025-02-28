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
public class GetLicenseByCurrentUserTest(DatabaseTestBase fixture) : IAsyncLifetime
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

    [Theory]
    [InlineData("Admin")]
    [InlineData("Consultant")]
    [InlineData("Technician")]
    public async Task Handle_UserNotDriverOrOwner_ReturnsForbidden(string roleName)
    {
        // Arrange
        var role = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, role);
        _currentUser.SetUser(testUser);

        var handler = new GetLicenseByCurrentUser.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetLicenseByCurrentUser.Query();

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

        var handler = new GetLicenseByCurrentUser.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetLicenseByCurrentUser.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy giấy phép lái xe", result.Errors);
    }

    [Theory]
    [InlineData("Driver")]
    [InlineData("Owner")]
    public async Task Handle_ValidRequest_ReturnsLicenseSuccessfully(string roleName)
    {
        // Arrange
        var role = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, role);
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
            UserId = testUser.Id,
            EncryptionKeyId = encryptionKey.Id,
            EncryptedLicenseNumber = encryptedLicenseNumber,
            ExpiryDate = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd"),
            LicenseImageFrontUrl = "front-url",
            LicenseImageBackUrl = "back-url",
        };
        await _dbContext.Licenses.AddAsync(license);
        await _dbContext.SaveChangesAsync();

        var handler = new GetLicenseByCurrentUser.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetLicenseByCurrentUser.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Lấy thông tin giấy phép lái xe thành công", result.SuccessMessage);

        var response = result.Value;
        Assert.Equal(license.Id, response.Id);
        Assert.Equal(licenseNumber, response.LicenseNumber); // Compare with original license number
        Assert.Equal(DateTimeOffset.Parse(license.ExpiryDate), response.ExpirationDate);
        Assert.Equal(license.LicenseImageFrontUrl, response.ImageFrontUrl);
        Assert.Equal(license.LicenseImageBackUrl, response.ImageBackUrl);
    }
}
