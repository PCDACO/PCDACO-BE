using Ardalis.Result;
using Moq;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_User.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_User.Commands;

[Collection("Test Collection")]
public class UploadUserAvatarTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;
    private readonly Mock<ICloudinaryServices> _cloudinaryServices = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    private UploadUserAvatar.Command CreateValidCommand(Guid userId)
    {
        var avatarStream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Valid JPEG signature
        return new UploadUserAvatar.Command(userId, avatarStream);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var handler = new UploadUserAvatar.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains("Người dùng không tồn tại", result.Errors);
    }

    [Fact]
    public async Task Handle_UnauthorizedAccess_ReturnsForbidden()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var targetUser = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        var differentUser = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            userRole,
            "different@example.com"
        );
        _currentUser.SetUser(differentUser);

        var handler = new UploadUserAvatar.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand(targetUser.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains("Không có quyền cập nhật ảnh đại diện của người dùng khác", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesAvatarSuccessfully()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);

        _cloudinaryServices
            .Setup(x =>
                x.UploadUserImageAsync(
                    It.Is<string>(s => s.Contains($"User-{user.Id}-Avatar")),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("new-avatar-url");

        var handler = new UploadUserAvatar.Handler(
            _dbContext,
            _currentUser,
            _cloudinaryServices.Object
        );

        var command = CreateValidCommand(user.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Cập nhật ảnh đại diện thành công", result.SuccessMessage);

        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("new-avatar-url", updatedUser.AvatarUrl);
        Assert.True(DateTimeOffset.UtcNow >= updatedUser.UpdatedAt);

        // Verify Cloudinary service was called correctly
        _cloudinaryServices.Verify(
            x =>
                x.UploadUserImageAsync(
                    It.Is<string>(s => s.Contains($"User-{user.Id}-Avatar")),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}
