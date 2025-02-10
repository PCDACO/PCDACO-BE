using Ardalis.Result;
using Domain.Shared;
using Moq;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_User.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_User.Queries;

[Collection("Test Collection")]
public class GetAllOwnersTests : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly Func<Task> _resetDatabase;
    private readonly Mock<IAesEncryptionService> _mockAesEncryptionService;
    private readonly Mock<IKeyManagementService> _mockKeyManagementService;
    private readonly EncryptionSettings _encryptionSettings;

    public GetAllOwnersTests(DatabaseTestBase fixture)
    {
        _dbContext = fixture.DbContext;
        _currentUser = fixture.CurrentUser;
        _resetDatabase = fixture.ResetDatabaseAsync;
        _mockAesEncryptionService = new();
        _mockKeyManagementService = new();
        _encryptionSettings = new() { Key = TestConstants.MasterKey };
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);
        var handler = new GetAllOwners.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );
        var query = new GetAllOwners.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsPaginatedOwners()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        var owners = await TestDataCreateUser.CreateTestUserList(_dbContext, ownerRole);

        _mockKeyManagementService
            .Setup(kms => kms.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("decryptedKey");
        _mockAesEncryptionService
            .Setup(aes => aes.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("1234567890");

        var handler = new GetAllOwners.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );
        var query = new GetAllOwners.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.Items.Count());

        // Ensure the owners are sorted by CreateAt
        var sortedOwners = result.Value.Items.OrderByDescending(u => u.Id).ToList();
        Assert.Equal(sortedOwners, result.Value.Items);
    }

    [Fact]
    public async Task Handle_KeywordFilter_ReturnsFilteredResults()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        var owners = await TestDataCreateUser.CreateTestUserList(_dbContext, ownerRole);

        _mockKeyManagementService
            .Setup(kms => kms.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("decryptedKey");
        _mockAesEncryptionService
            .Setup(aes => aes.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("1234567890");

        var handler = new GetAllOwners.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );
        var query = new GetAllOwners.Query(Keyword: "User One");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal("User One", result.Value.Items.First().Name);
    }

    [Fact]
    public async Task Handle_PaginationAndSorting_ReturnsCorrectPageAndSortedList()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var ownerRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Owner");
        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        var owners = await TestDataCreateUser.CreateTestUserList(_dbContext, ownerRole);

        _mockKeyManagementService
            .Setup(kms => kms.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("decryptedKey");
        _mockAesEncryptionService
            .Setup(aes => aes.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("1234567890");

        var handler = new GetAllOwners.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );

        // Act
        var query = new GetAllOwners.Query(PageNumber: 2, PageSize: 2);
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);

        // Ensure the correct owners are returned for the second page
        Assert.Equal("User One", result.Value.Items.FirstOrDefault()?.Name);
        Assert.Equal("User One", result.Value.Items.LastOrDefault()?.Name);
    }
}
