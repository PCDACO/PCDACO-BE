using Ardalis.Result;
using Domain.Constants;
using Moq;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Manufacturer.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Manufacturer.Commands;

[Collection("Test Collection")]
public class UploadManufacturerLogoTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly Mock<ICloudinaryServices> _cloudinaryServices = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    private UploadManufacturerLogo.Command CreateValidCommand(Guid manufacturerId)
    {
        var logoStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Valid JPEG signature
        return new UploadManufacturerLogo.Command(manufacturerId, logoStream);
    }

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new UploadManufacturerLogo.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_ManufacturerNotFound_ReturnsNotFound()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new UploadManufacturerLogo.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy hãng xe", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_UploadsLogoSuccessfully()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var oldLogoUrl = manufacturer.LogoUrl;

        _cloudinaryServices
            .Setup(x =>
                x.UploadManufacturerLogoAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("https://cloudinary.com/new-logo.png");

        var handler = new UploadManufacturerLogo.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand(manufacturer.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains("Cập nhật logo hãng xe thành công", result.SuccessMessage);

        // Verify logo URL was updated
        var updatedManufacturer = await _dbContext.Manufacturers.FindAsync(manufacturer.Id);
        Assert.NotNull(updatedManufacturer);
        Assert.Equal("https://cloudinary.com/new-logo.png", updatedManufacturer.LogoUrl);
        Assert.NotEqual(oldLogoUrl, updatedManufacturer.LogoUrl);

        // Verify Cloudinary service was called
        _cloudinaryServices.Verify(
            x =>
                x.UploadManufacturerLogoAsync(
                    It.Is<string>(s => s.Contains($"Manufacturer-{manufacturer.Id}-Logo")),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void Validator_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new UploadManufacturerLogo.Validator();
        var command = new UploadManufacturerLogo.Command(Guid.Empty, null!);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ManufacturerId");
        Assert.Contains(result.Errors, e => e.PropertyName == "LogoImage");
    }
}
