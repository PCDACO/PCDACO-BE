using Ardalis.Result;
using Domain.Shared;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_License.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_License.Commands;

[Collection("Test Collection")]
public class ApproveLicenseTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly IAesEncryptionService _aesService = fixture.AesEncryptionService;
    private readonly IKeyManagementService _keyService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new ApproveLicense.Handler(_dbContext, _currentUser);
        var command = new ApproveLicense.Command(Guid.NewGuid(), true);

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
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(admin);

        var handler = new ApproveLicense.Handler(_dbContext, _currentUser);
        var command = new ApproveLicense.Command(Guid.NewGuid(), true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy giấy phép lái xe", result.Errors);
    }

    [Fact]
    public async Task Handle_AlreadyProcessed_ReturnsConflict()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(admin);

        // Create an already processed license
        var license = await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            driver.Id,
            _aesService,
            _keyService,
            _encryptionSettings
        );
        license.IsApprove = true;
        await _dbContext.SaveChangesAsync();

        var handler = new ApproveLicense.Handler(_dbContext, _currentUser);
        var command = new ApproveLicense.Command(license.Id, true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("Giấy phép này đã được xử lý", result.Errors);
    }

    [Fact]
    public async Task Handle_RejectWithoutReason_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(admin);

        var license = await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            driver.Id,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var handler = new ApproveLicense.Handler(_dbContext, _currentUser);
        var command = new ApproveLicense.Command(license.Id, false); // No reject reason

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Phải cung cấp lý do từ chối giấy phép", result.Errors);
    }

    [Theory]
    [InlineData(true, null, "phê duyệt")] // Approve case
    [InlineData(false, "Invalid license", "từ chối")] // Reject case
    public async Task Handle_ValidRequest_UpdatesLicenseSuccessfully(
        bool isApproved,
        string? rejectReason,
        string expectedMessage
    )
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var admin = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        var driver = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(admin);

        var license = await TestDataCreateLicense.CreateTestLicense(
            _dbContext,
            driver.Id,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var handler = new ApproveLicense.Handler(_dbContext, _currentUser);
        var command = new ApproveLicense.Command(license.Id, isApproved, rejectReason);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains($"Đã {expectedMessage} giấy phép lái xe thành công", result.SuccessMessage);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        Assert.NotNull(updatedLicense);
        Assert.Equal(isApproved, updatedLicense.IsApprove);
        Assert.Equal(rejectReason, updatedLicense.RejectReason);
        if (isApproved)
        {
            Assert.NotNull(updatedLicense.ApprovedAt);
        }
        else
        {
            Assert.Null(updatedLicense.ApprovedAt);
        }
    }

    [Fact]
    public void Validator_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new ApproveLicense.Validator();
        var command = new ApproveLicense.Command(Guid.Empty, false); // Empty ID and no reject reason

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LicenseId");
        Assert.Contains(result.Errors, e => e.PropertyName == "RejectReason");
    }
}
