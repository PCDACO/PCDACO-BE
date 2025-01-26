using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using UseCases.UC_Amenity.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_Amenity.Commands;

public class CreateAmenityTest : DatabaseTestBase
{
    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        UserRole driverRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var testUser = await TestDataCreateUser.CreateTestUser(_dbContext, driverRole);
        _currentUser.SetUser(testUser);

        var handler = new CreateAmenity.Handler(_dbContext, _currentUser);
        var command = new CreateAmenity.Command("WiFi", "High-speed internet");

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

        var handler = new CreateAmenity.Handler(_dbContext, _currentUser);
        var command = new CreateAmenity.Command("Pool", "Swimming pool access");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Created, result.Status);

        // Verify using factory-created pattern
        var createdAmenity = await _dbContext.Amenities.FirstOrDefaultAsync(a => a.Name == "Pool");

        Assert.NotNull(createdAmenity);
        Assert.Equal("Swimming pool access", createdAmenity.Description);
    }
}
