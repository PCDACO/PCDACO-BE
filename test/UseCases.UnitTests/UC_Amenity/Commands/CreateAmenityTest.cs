using Ardalis.Result;

using Domain.Entities;

using Microsoft.EntityFrameworkCore;

using Moq;

using Persistance.Data;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_Amenity.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Amenity.Commands;

[Collection("Test Collection")]
public class CreateAmenityTest : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly Func<Task> _resetDatabase;
    private readonly Mock<ICloudinaryServices> _cloudinaryServices;
    private const string ExpectedImageUrl = "https://cloudinary.com/test-image.jpg";

    public CreateAmenityTest(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _currentUser = fixture.CurrentUser;
        _resetDatabase = fixture.ResetDatabaseAsync;
        _cloudinaryServices = new Mock<ICloudinaryServices>();

        _cloudinaryServices
            .Setup(x =>
                x.UploadAmenityIconAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(ExpectedImageUrl);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new CreateAmenity.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );
        var command = new CreateAmenity.Command("WiFi", "High-speed internet", new MemoryStream());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện thao tác này", result.Errors);
    }

    [Fact]
    public async Task Handle_AdminUser_CreatesAmenitySuccessfully()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new CreateAmenity.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );
        var command = new CreateAmenity.Command("Pool", "Swimming pool access", new MemoryStream());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Created, result.Status);

        // Verify using factory-created pattern
        var createdAmenity = await _dbContext.Amenities.FirstOrDefaultAsync(a => a.Name == "Pool");

        Assert.NotNull(createdAmenity);
        Assert.Equal("Swimming pool access", createdAmenity.Description);

        // Verify that Cloudinary upload was called if image was provided
        _cloudinaryServices.Verify(
            x =>
                x.UploadAmenityIconAsync(
                    It.IsAny<string>(),
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
        var expectedErrors = new[]
        {
            "tên không được để trống",
            "mô tả không được để trống",
            "Biểu tượng không được để trống",
            "Biểu tượng không được vượt quá 10MB",
            $"Chỉ chấp nhận các định dạng: .jpg, .jpeg, .png, .gif, .bmp, .tiff, .webp",
        };
        var validator = new CreateAmenity.Validator();
        Stream invalidStream = new MemoryStream(new byte[11 * 1024 * 1024]);
        var command = new CreateAmenity.Command(
            Name: "", // Empty name
            Description: "", // Empty description
            Icon: new MemoryStream() // Invalid size stream
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.All(result.Errors, error => Assert.Contains(error.ErrorMessage, expectedErrors));
        invalidStream.Dispose();
    }
}
