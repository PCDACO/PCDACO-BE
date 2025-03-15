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
public class GetLicenseByUserIdTest(DatabaseTestBase fixture) : IAsyncLifetime
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
    public async Task Handle_UserNotAdmin_ReturnsForbidden(string roleName)
    {
        // Arrange
        var role = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, role);
        _currentUser.SetUser(testUser);

        var handler = new GetLicenseByUserId.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetLicenseByUserId.Query(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này", result.Errors);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new GetLicenseByUserId.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetLicenseByUserId.Query(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Người dùng không tồn tại", result.Errors);
    }

    [Fact]
    public async Task Handle_LicenseNotFound_ReturnsNotFound()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new GetLicenseByUserId.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetLicenseByUserId.Query(testUser.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy giấy phép lái xe", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsLicenseSuccessfully()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");

        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        var differentUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
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

        // Add License
        var updateDriver = await _dbContext.Users.FindAsync(differentUser.Id);
        updateDriver!.EncryptionKeyId = encryptionKey.Id;
        updateDriver.EncryptedLicenseNumber = encryptedLicenseNumber;
        updateDriver.LicenseExpiryDate = DateTimeOffset.UtcNow.AddYears(1);
        updateDriver.LicenseImageFrontUrl = "front-url";
        updateDriver.LicenseImageBackUrl = "back-url";

        await _dbContext.SaveChangesAsync();

        var handler = new GetLicenseByUserId.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        var query = new GetLicenseByUserId.Query(differentUser.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Lấy thông tin giấy phép lái xe thành công", result.SuccessMessage);

        var response = result.Value;
        Assert.Equal(updateDriver.Id, response.UserId);
        Assert.Equal(licenseNumber, response.LicenseNumber);
        Assert.Equal(
            updateDriver.LicenseExpiryDate.Value.Date,
            response.ExpirationDate!.Value.Date
        );
        Assert.Equal(updateDriver.LicenseImageFrontUrl, response.ImageFrontUrl);
        Assert.Equal(updateDriver.LicenseImageBackUrl, response.ImageBackUrl);
    }
}
