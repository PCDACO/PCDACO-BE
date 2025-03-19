using Ardalis.Result;
using Domain.Shared;
using Infrastructure.Encryption;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_License.Commands;
using UseCases.UC_User.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_License.Commands;

[Collection("Test Collection")]
public class AddUserLicenseTest : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly Func<Task> _resetDatabase;
    private readonly EncryptionSettings _encryptionSettings;
    private readonly IAesEncryptionService _aesService;
    private readonly IKeyManagementService _keyService;

    public AddUserLicenseTest(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _currentUser = fixture.CurrentUser;
        _resetDatabase = fixture.ResetDatabaseAsync;
        _encryptionSettings = new EncryptionSettings { Key = TestConstants.MasterKey };
        _aesService = new AesEncryptionService();
        _keyService = new KeyManagementService();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    private AddUserLicense.Command CreateValidCommand() =>
        new(LicenseNumber: "123456789012", ExpirationDate: DateTime.UtcNow.AddYears(1));

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

        var handler = new AddUserLicense.Handler(
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
    public async Task Handle_AlreadyHasLicense_ReturnsError()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(driver);

        // Create existing license
        var userWithExistingLicense = await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            driver.Id,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var handler = new AddUserLicense.Handler(
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
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Người dùng đã có giấy phép lái xe", result.Errors);
    }

    [Fact]
    public async Task Handle_DuplicateLicenseNumber_ReturnsError()
    {
        // Arrange
        // Create first driver with a license
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver1 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver1@example.com"
        );

        string existingLicenseNumber = "123456789012";
        var userWithExistingLicense = await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            driver1.Id,
            _aesService,
            _keyService,
            _encryptionSettings,
            existingLicenseNumber
        );

        // Create second driver who will try to use the same license number
        var driver2 = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            driverRole,
            "driver2@example.com"
        );
        _currentUser.SetUser(driver2);

        var handler = new AddUserLicense.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var command = new AddUserLicense.Command(
            LicenseNumber: existingLicenseNumber,
            ExpirationDate: DateTime.UtcNow.AddYears(1)
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Số giấy phép lái xe đã tồn tại", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesLicenseSuccessfully()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(driver);

        var handler = new AddUserLicense.Handler(
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
        Assert.Equal("Thêm giấy phép lái xe thành công", result.SuccessMessage);
        Assert.NotNull(result.Value);

        // Verify license was created in database
        var user = await _dbContext.Users.FindAsync(result.Value.UserId);
        Assert.NotNull(user);
        Assert.Equal(driver.Id, user.Id);
        Assert.Equal(command.ExpirationDate.Date, user.LicenseExpiryDate!.Value.Date);
    }
}
