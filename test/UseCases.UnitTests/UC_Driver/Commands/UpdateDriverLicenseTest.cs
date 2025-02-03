using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Moq;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Driver.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Driver.Commands;

[Collection("Test Collection")]
public class UpdateDriverLicenseTest : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly Func<Task> _resetDatabase;
    private readonly EncryptionSettings _encryptionSettings;
    private readonly IAesEncryptionService _aesService;
    private readonly IKeyManagementService _keyService;
    private readonly Mock<ICloudinaryServices> _cloudinaryServices;

    public UpdateDriverLicenseTest(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _currentUser = fixture.CurrentUser;
        _resetDatabase = fixture.ResetDatabaseAsync;

        _encryptionSettings = new EncryptionSettings { Key = TestConstants.MasterKey };
        _aesService = new AesEncryptionService();
        _keyService = new KeyManagementService();
        _cloudinaryServices = new Mock<ICloudinaryServices>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    private async Task<UpdateDriverLicense.Command> CreateValidCommand()
    {
        var frontImageStream = new MemoryStream(new byte[100]);
        var backImageStream = new MemoryStream(new byte[100]);

        return new UpdateDriverLicense.Command(
            LicenseNumber: "123456789012",
            LicenseImageFrontUrl: frontImageStream,
            LicenseImageBackUrl: backImageStream,
            Fullname: "Test Driver",
            ExpirationDate: DateTime.UtcNow.AddYears(1)
        );
    }

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
            _encryptionSettings,
            _cloudinaryServices.Object
        );

        var command = await CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesDriverLicenseSuccessfully()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        _cloudinaryServices
            .Setup(x =>
                x.UploadDriverLicenseImageAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("test-image-url");

        var handler = new UpdateDriverLicense.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings,
            _cloudinaryServices.Object
        );

        var command = await CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Cập nhật giấy phép lái xe thành công", result.SuccessMessage);

        // Verify database state
        var driver = await _dbContext
            .Drivers.Include(d => d.EncryptionKey)
            .FirstOrDefaultAsync(d => d.UserId == testUser.Id);

        Assert.NotNull(driver);
        Assert.Equal("test-image-url", driver.LicenseImageFrontUrl);
        Assert.Equal("test-image-url", driver.LicenseImageBackUrl);
        Assert.Equal("Test Driver", driver.Fullname);
        Assert.Null(driver.IsApprove);
    }

    [Fact]
    public async Task Handle_UpdateExistingLicense_UpdatesSuccessfully()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        // Create existing encryption key
        var encryptionKey = await TestDataCreateEncryptionKey.CreateTestEncryptionKey(_dbContext);

        // Create existing driver license
        var existingDriver = new Driver
        {
            UserId = testUser.Id,
            EncryptedLicenseNumber = "old-number",
            EncryptionKeyId = encryptionKey.Id,
            LicenseImageFrontUrl = "old-front-url",
            LicenseImageBackUrl = "old-back-url",
            Fullname = "Old Name",
            ExpiryDate = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
            IsApprove = true,
        };
        await _dbContext.Drivers.AddAsync(existingDriver);
        await _dbContext.SaveChangesAsync();

        _cloudinaryServices
            .Setup(x =>
                x.UploadDriverLicenseImageAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("new-image-url");

        var handler = new UpdateDriverLicense.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings,
            _cloudinaryServices.Object
        );

        var command = await CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        var updatedDriver = await _dbContext.Drivers.FirstAsync(d => d.UserId == testUser.Id);
        Assert.Equal("new-image-url", updatedDriver.LicenseImageFrontUrl);
        Assert.Equal("new-image-url", updatedDriver.LicenseImageBackUrl);
        Assert.Equal("Test Driver", updatedDriver.Fullname);
        Assert.Null(updatedDriver.IsApprove);
    }

    [Fact]
    public async Task Validator_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new UpdateDriverLicense.Validator();
        var command = new UpdateDriverLicense.Command(
            LicenseNumber: "123", // Invalid length
            LicenseImageFrontUrl: null!, // Missing image
            LicenseImageBackUrl: null!, // Missing image
            Fullname: "", // Empty name
            ExpirationDate: DateTime.UtcNow.AddDays(-1) // Past date
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LicenseNumber");
        Assert.Contains(result.Errors, e => e.PropertyName == "LicenseImageFrontUrl");
        Assert.Contains(result.Errors, e => e.PropertyName == "LicenseImageBackUrl");
        Assert.Contains(result.Errors, e => e.PropertyName == "Fullname");
        Assert.Contains(result.Errors, e => e.PropertyName == "ExpirationDate");
    }
}
