using Ardalis.Result;
using Domain.Constants;
using Persistance.Data;
using UseCases.DTOs;
using UseCases.UC_Auth.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;

namespace UseCases.UnitTests.UC_Auth.Commands;

[Collection("Test Collection")]
public class ResetPasswordTest : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly Func<Task> _resetDatabase;

    public ResetPasswordTest(DatabaseTestBase fixture)
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

        var handler = new ResetPassword.Handler(_dbContext, _currentUser);
        var command = new ResetPassword.Command("newpassword123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Cập nhật mật khẩu thành công", result.SuccessMessage);

        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("newpassword123".HashString(), updatedUser.Password);
        Assert.NotNull(updatedUser.UpdatedAt);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);

        // Delete the user to simulate not found
        user.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        var handler = new ResetPassword.Handler(_dbContext, _currentUser);
        var command = new ResetPassword.Command("newpassword123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.UserNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_SamePassword_ReturnsError()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);

        // Try to set the same password (the default is "password")
        var handler = new ResetPassword.Handler(_dbContext, _currentUser);
        var command = new ResetPassword.Command("password");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Đây là mật khẩu cũ của bạn", result.Errors);

        // Verify password was not changed
        var unchangedUser = await _dbContext.Users.FindAsync(user.Id);
        Assert.NotNull(unchangedUser);
        Assert.Equal(user.Password, unchangedUser.Password);
    }

    [Theory]
    [InlineData("", "Mật khẩu mới không được để trống")]
    [InlineData("short", "Mật khẩu mới phải có ít nhất 6 ký tự")]
    public void Validator_InvalidPassword_ReturnsValidationErrors(
        string password,
        string expectedError
    )
    {
        // Arrange
        var validator = new ResetPassword.Validator();
        var command = new ResetPassword.Command(password);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(expectedError, result.Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public void Validator_ValidPassword_PassesValidation()
    {
        // Arrange
        var validator = new ResetPassword.Validator();
        var command = new ResetPassword.Command("validpassword123");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
