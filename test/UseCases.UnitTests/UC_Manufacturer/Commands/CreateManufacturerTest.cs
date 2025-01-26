using Ardalis.Result;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.UC_Manufacturer.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Manufacturer.Commands;

public class CreateManufacturerTest : DatabaseTestBase
{
    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsError()
    {
        // Arrange
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Driver);
        _currentUser.SetUser(testUser);

        var handler = new CreateManufacturer.Handler(_dbContext, _currentUser);
        var command = new CreateManufacturer.Command("Toyota");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Bạn không có quyền thực hiện chức năng này !", result.Errors);
    }

    [Fact]
    public async Task Handle_AdminUser_CreatesManufacturerSuccessfully()
    {
        // Arrange
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, UserRole.Admin);
        _currentUser.SetUser(testUser);

        var handler = new CreateManufacturer.Handler(_dbContext, _currentUser);
        var command = new CreateManufacturer.Command("Toyota");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify sử dụng TestData pattern
        var createdManufacturer = await _dbContext.Manufacturers.FirstOrDefaultAsync(m =>
            m.Name == "Toyota"
        );

        Assert.NotNull(createdManufacturer);
    }
}
