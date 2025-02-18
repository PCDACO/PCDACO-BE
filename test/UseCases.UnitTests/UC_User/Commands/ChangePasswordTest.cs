using Ardalis.Result;
using Domain.Constants;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_User.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;

namespace UseCases.UnitTests.UC_User.Commands;

[Collection("Test Collection")]
public class ChangePasswordTest : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly Func<Task> _resetDatabase;

    public ChangePasswordTest(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _currentUser = fixture.CurrentUser;
        _resetDatabase = fixture.ResetDatabaseAsync;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Theory]
    [InlineData("Owner")]
    [InlineData("Admin")]
    [InlineData("Driver")]
    [InlineData("Consultant")]
    [InlineData("Technician")]
    public async Task Handle_ValidRequest_ChangesPasswordSuccessfully(string roleName)
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);

        var handler = new ChangePassword.Handler(_dbContext, _currentUser);
        var command = new ChangePassword.Command(
            user.Id,
            "password", // Original password from TestDataCreateUser
            "newpassword123"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(ResponseMessages.Updated, result.SuccessMessage);

        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("newpassword123".HashString(), updatedUser.Password);
        Assert.Equal(roleName, updatedUser.Role.Name);
    }

    [Fact]
    public async Task Handle_InvalidOldPassword_ReturnsError()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);

        var handler = new ChangePassword.Handler(_dbContext, _currentUser);
        var command = new ChangePassword.Command(user.Id, "wrongpassword", "newpassword123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(ResponseMessages.OldPasswordIsInvalid, result.Errors);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);

        var handler = new ChangePassword.Handler(_dbContext, _currentUser);
        var command = new ChangePassword.Command(
            Guid.NewGuid(), // Non-existent user ID
            "password",
            "newpassword123"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.UserNotFound, result.Errors);
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

        var handler = new ChangePassword.Handler(_dbContext, _currentUser);
        var command = new ChangePassword.Command(targetUser.Id, "password", "newpassword123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public void Validator_InvalidPasswords_ReturnsValidationErrors()
    {
        // Arrange
        var validator = new ChangePassword.Validator();
        var command = new ChangePassword.Command(
            Guid.NewGuid(),
            "", // Empty old password
            "123" // Too short new password
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "OldPassword");
        Assert.Contains(result.Errors, e => e.PropertyName == "NewPassword");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Mật khẩu cũ không được để trống");
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage == "Mật khẩu mới phải có ít nhất 6 ký tự"
        );
    }
}
