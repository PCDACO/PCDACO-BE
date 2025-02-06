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
using UUIDNext;

namespace UseCases.UnitTests.UC_Amenity.Commands;

[Collection("Test Collection")]
public class UpdateAmenityTests : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly Func<Task> _resetDatabase;
    private readonly Mock<ICloudinaryServices> _cloudinarySerives;

    private const string ExpectedImageUrl = "https://cloudinary.com/test-image.jpg";

    public UpdateAmenityTests(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _currentUser = fixture.CurrentUser;
        _resetDatabase = fixture.ResetDatabaseAsync;
        _cloudinarySerives = new Mock<ICloudinaryServices>();

        _cloudinarySerives
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

        var handler = new UpdateAmenity.Handler(
            _dbContext,
            _currentUser,
            _cloudinarySerives.Object
        );
        var command = new UpdateAmenity.Command(
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            "New Name",
            "New Description"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện thao tác này", result.Errors);
    }

    [Fact]
    public async Task Handle_AmenityNotFound_ReturnsNotFound()
    {
        // Arrange
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);

        var handler = new UpdateAmenity.Handler(
            _dbContext,
            _currentUser,
            _cloudinarySerives.Object
        );
        var command = new UpdateAmenity.Command(
            Uuid.NewDatabaseFriendly(Database.PostgreSql),
            "New Name",
            "New Description"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Không tìm thấy tiện nghi", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesAmenitySuccessfully()
    {
        // Arrange
        using var updateImageStream = new MemoryStream();
        UserRole adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(testUser);
        var testAmenity = await TestDataCreateAmenity.CreateTestAmenity(
            _dbContext,
            isDeleted: false
        );

        var handler = new UpdateAmenity.Handler(
            _dbContext,
            _currentUser,
            _cloudinarySerives.Object
        );
        var command = new UpdateAmenity.Command(
            testAmenity.Id,
            "Updated Name",
            "Updated Description",
            updateImageStream
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Contains("Cập nhật tiện nghi thành công", result.SuccessMessage);

        var updatedAmenity = await _dbContext
            .Amenities.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == testAmenity.Id);

        Assert.NotNull(updatedAmenity);
        Assert.Equal("Updated Name", updatedAmenity.Name);
        Assert.Equal("Updated Description", updatedAmenity.Description);
        Assert.True(DateTimeOffset.UtcNow >= updatedAmenity.UpdatedAt);

        // Verify that Cloudinary service was called for image upload
        _cloudinarySerives.Verify(
            x =>
                x.UploadAmenityIconAsync(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once // Ensures upload was called only once
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
            "Biểu tượng không được vượt quá 10MB",
        };

        var validator = new UpdateAmenity.Validator();
        var invalidStream = new MemoryStream(new byte[11 * 1024 * 1024]); // Create oversized stream
        var command = new UpdateAmenity.Command(
            Id: Guid.NewGuid(),
            Name: "", // Empty name
            Description: "", // Empty description
            Icon: invalidStream // Invalid size stream
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.All(result.Errors, error => Assert.Contains(error.ErrorMessage, expectedErrors));

        // Cleanup
        invalidStream.Dispose();
    }
}
