using Ardalis.Result;
using Domain.Entities;
using Moq;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_License.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_License.Commands;

[Collection("Test Collection")]
public class UploadUserLicenseImageTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly Mock<ICloudinaryServices> _cloudinaryServices = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    private UploadUserLicenseImage.Command CreateValidCommand()
    {
        var frontImageStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Valid JPEG signature
        var backImageStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Valid JPEG signature

        return new UploadUserLicenseImage.Command(
            LicenseImageFrontUrl: frontImageStream,
            LicenseImageBackUrl: backImageStream
        );
    }

    [Fact]
    public async Task Handle_UserNotDriver_ReturnsError()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new UploadUserLicenseImage.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này", result.Errors);
    }

    [Fact]
    public async Task Handle_LicenseNotFound_ReturnsNotFound()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new UploadUserLicenseImage.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
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
    public async Task Handle_ValidRequest_UpdatesImagesSuccessfully(string roleName)
    {
        // Arrange
        var role = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, role);
        _currentUser.SetUser(testUser);

        var updateUser = await _dbContext.Users.FindAsync(testUser.Id);
        updateUser!.EncryptedLicenseNumber = "NTjmhIE3YJtqsqXCZYbjzA==";
        updateUser!.LicenseImageFrontUrl = "old-front-url";
        updateUser.LicenseImageBackUrl = "old-back-url";
        updateUser.LicenseExpiryDate = DateTimeOffset.UtcNow.AddDays(1);

        await _dbContext.SaveChangesAsync();

        _cloudinaryServices
            .Setup(x =>
                x.UploadDriverLicenseImageAsync(
                    It.Is<string>(s => s.Contains("FrontImage")),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("new-front-url");

        _cloudinaryServices
            .Setup(x =>
                x.UploadDriverLicenseImageAsync(
                    It.Is<string>(s => s.Contains("BackImage")),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("new-back-url");

        var handler = new UploadUserLicenseImage.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Cập nhật ảnh giấy phép lái xe thành công", result.SuccessMessage);

        var userUploadedLicenseImage = await _dbContext.Users.FindAsync(testUser.Id);
        Assert.NotNull(userUploadedLicenseImage);
        Assert.NotEmpty(userUploadedLicenseImage.EncryptedLicenseNumber);
        Assert.Equal("new-front-url", userUploadedLicenseImage.LicenseImageFrontUrl);
        Assert.Equal("new-back-url", userUploadedLicenseImage.LicenseImageBackUrl);
    }

    [Fact]
    public void Validator_InvalidImages_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new UploadUserLicenseImage.Validator();
        var command = new UploadUserLicenseImage.Command(
            LicenseImageFrontUrl: null!,
            LicenseImageBackUrl: null!
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LicenseImageFrontUrl");
        Assert.Contains(result.Errors, e => e.PropertyName == "LicenseImageBackUrl");
    }
}
