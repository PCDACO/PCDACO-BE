using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using Moq;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_User.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;
using UseCases.Utils;

namespace UseCases.UnitTests.UC_User.Queries;

[Collection("Test Collection")]
public class GetAllTechniciansTest : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly Func<Task> _resetDatabase;
    private readonly Mock<IAesEncryptionService> _mockAesEncryptionService;
    private readonly Mock<IKeyManagementService> _mockKeyManagementService;
    private readonly EncryptionSettings _encryptionSettings;

    public GetAllTechniciansTest(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _currentUser = fixture.CurrentUser;
        _resetDatabase = fixture.ResetDatabaseAsync;
        _mockAesEncryptionService = new Mock<IAesEncryptionService>();
        _mockKeyManagementService = new Mock<IKeyManagementService>();
        _encryptionSettings = new EncryptionSettings() { Key = TestConstants.MasterKey };
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Theory]
    [InlineData("Driver")]
    [InlineData("Owner")]
    public async Task Handle_UserNotAdminOrConsultant_ReturnsForbidden(string roleName)
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);

        var handler = new GetAllTechnicians.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );

        var query = new GetAllTechnicians.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Consultant")]
    public async Task Handle_UserIsAdminOrConsultant_AllowsAccess(string roleName)
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var adminOrConsultant = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(adminOrConsultant);

        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );
        await TestDataCreateUser.CreateTestUser(_dbContext, technicianRole);

        _mockKeyManagementService
            .Setup(kms => kms.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("decryptedKey");
        _mockAesEncryptionService
            .Setup(aes => aes.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("1234567890");

        var handler = new GetAllTechnicians.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );

        var query = new GetAllTechnicians.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Handle_NoTechnicians_ReturnsEmptyList()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        _mockKeyManagementService
            .Setup(kms => kms.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("decryptedKey");
        _mockAesEncryptionService
            .Setup(aes => aes.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("1234567890");

        var handler = new GetAllTechnicians.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );

        var query = new GetAllTechnicians.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
        Assert.False(result.Value.HasNext);
    }

    [Fact]
    public async Task Handle_WithTechnicians_ReturnsPaginatedTechnicians()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );

        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        // Create multiple technicians
        var technicians = new List<User>();
        for (int i = 1; i <= 3; i++)
        {
            var tech = await TestDataCreateUser.CreateTestUser(
                _dbContext,
                technicianRole,
                $"tech{i}@example.com",
                $"Technician {i}",
                $"123456789{i}"
            );
            technicians.Add(tech);
        }

        _mockKeyManagementService
            .Setup(kms => kms.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("decryptedKey");
        _mockAesEncryptionService
            .Setup(aes => aes.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("1234567890");

        var handler = new GetAllTechnicians.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );

        var query = new GetAllTechnicians.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.Items.Count());
        Assert.Equal(3, result.Value.TotalItems);
        Assert.False(result.Value.HasNext);
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);

        // Ensure the technicians are sorted by ID in descending order
        var sortedTechnicians = result.Value.Items.OrderByDescending(u => u.Id).ToList();
        Assert.Equal(sortedTechnicians, result.Value.Items);
    }

    [Fact]
    public async Task Handle_KeywordFilter_ReturnsFilteredResults()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );

        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        // Create technicians with different names and emails with mixed case
        await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech1@example.com",
            "Match Technician",
            "1234567891"
        );
        await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech2@example.com",
            "Other Technician",
            "1234567892"
        );
        await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "MATCH@example.com", // Uppercase email
            "Another MATCH Tech", // Uppercase in name
            "1234567893"
        );

        _mockKeyManagementService
            .Setup(kms => kms.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("decryptedKey");
        _mockAesEncryptionService
            .Setup(aes => aes.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("1234567890");

        var handler = new GetAllTechnicians.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );

        // Test case-insensitive name filter
        var nameQuery = new GetAllTechnicians.Query(Keyword: "match");
        var nameResult = await handler.Handle(nameQuery, CancellationToken.None);

        // Test case-insensitive email filter
        var emailQuery = new GetAllTechnicians.Query(Keyword: "MATCH@");
        var emailResult = await handler.Handle(emailQuery, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, nameResult.Status);
        Assert.Equal(2, nameResult.Value.Items.Count()); // Should find both "Match Technician" and "Another MATCH Tech"
        Assert.Contains(nameResult.Value.Items, x => x.Name == "Match Technician");
        Assert.Contains(nameResult.Value.Items, x => x.Name == "Another MATCH Tech");

        Assert.Equal(ResultStatus.Ok, emailResult.Status);
        Assert.Single(emailResult.Value.Items);
        Assert.Equal("MATCH@example.com", emailResult.Value.Items.First().Email);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPages()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );

        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        // Create 5 technicians
        for (int i = 1; i <= 5; i++)
        {
            await TestDataCreateUser.CreateTestUser(
                _dbContext,
                technicianRole,
                $"tech{i}@example.com",
                $"Technician {i}",
                $"123456789{i}"
            );
        }

        _mockKeyManagementService
            .Setup(kms => kms.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("decryptedKey");
        _mockAesEncryptionService
            .Setup(aes => aes.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("1234567890");

        var handler = new GetAllTechnicians.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );

        // First page (2 items)
        var firstPageQuery = new GetAllTechnicians.Query(PageNumber: 1, PageSize: 2);
        var firstPageResult = await handler.Handle(firstPageQuery, CancellationToken.None);

        // Second page (2 items)
        var secondPageQuery = new GetAllTechnicians.Query(PageNumber: 2, PageSize: 2);
        var secondPageResult = await handler.Handle(secondPageQuery, CancellationToken.None);

        // Third page (1 item)
        var thirdPageQuery = new GetAllTechnicians.Query(PageNumber: 3, PageSize: 2);
        var thirdPageResult = await handler.Handle(thirdPageQuery, CancellationToken.None);

        // Assert
        // First page assertions
        Assert.Equal(ResultStatus.Ok, firstPageResult.Status);
        Assert.Equal(2, firstPageResult.Value.Items.Count());
        Assert.Equal(5, firstPageResult.Value.TotalItems);
        Assert.True(firstPageResult.Value.HasNext);

        // Second page assertions
        Assert.Equal(ResultStatus.Ok, secondPageResult.Status);
        Assert.Equal(2, secondPageResult.Value.Items.Count());
        Assert.Equal(5, secondPageResult.Value.TotalItems);
        Assert.True(secondPageResult.Value.HasNext);

        // Third page assertions
        Assert.Equal(ResultStatus.Ok, thirdPageResult.Status);
        Assert.Single(thirdPageResult.Value.Items);
        Assert.Equal(5, thirdPageResult.Value.TotalItems);
        Assert.False(thirdPageResult.Value.HasNext);

        // Ensure different items are returned on each page
        var firstPageIds = firstPageResult.Value.Items.Select(i => i.Id).ToList();
        var secondPageIds = secondPageResult.Value.Items.Select(i => i.Id).ToList();
        var thirdPageIds = thirdPageResult.Value.Items.Select(i => i.Id).ToList();

        Assert.Empty(firstPageIds.Intersect(secondPageIds));
        Assert.Empty(firstPageIds.Intersect(thirdPageIds));
        Assert.Empty(secondPageIds.Intersect(thirdPageIds));
    }

    [Fact]
    public async Task Handle_ResponseContainsExpectedFields()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var technicianRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Technician"
        );

        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        // Create a technician with specific data
        var technician = await TestDataCreateUser.CreateTestUser(
            _dbContext,
            technicianRole,
            "tech@example.com",
            "John Technician",
            "9876543210",
            "http://example.com/tech.jpg"
        );

        var expectedPhone = "9876543210";
        _mockKeyManagementService
            .Setup(kms => kms.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("decryptedKey");
        _mockAesEncryptionService
            .Setup(aes => aes.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedPhone);

        var handler = new GetAllTechnicians.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );

        var query = new GetAllTechnicians.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        var techData = result.Value.Items.Single();

        Assert.Equal(technician.Id, techData.Id);
        Assert.Equal("John Technician", techData.Name);
        Assert.Equal("tech@example.com", techData.Email);
        Assert.Equal("Test Address", techData.Address);
        Assert.Equal(expectedPhone, techData.Phone);
        Assert.Equal("Technician", techData.Role);
        Assert.Equal(technician.DateOfBirth.Date, techData.DateOfBirth.Date);
    }
}
