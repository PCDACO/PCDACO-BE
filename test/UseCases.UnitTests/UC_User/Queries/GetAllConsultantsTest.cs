using Ardalis.Result;
using Domain.Constants;
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
public class GetAllConsultantsTests : IAsyncLifetime
{
    private readonly AppDBContext _dbContext;
    private readonly CurrentUser _currentUser;
    private readonly Func<Task> _resetDatabase;
    private readonly Mock<IAesEncryptionService> _mockAesEncryptionService;
    private readonly Mock<IKeyManagementService> _mockKeyManagementService;
    private readonly EncryptionSettings _encryptionSettings;

    public GetAllConsultantsTests(DatabaseTestBase fixture)
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

    [Fact]
    public async Task Handle_UserNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Consultant");
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);
        var handler = new GetAllConsultants.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );
        var query = new GetAllConsultants.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsPaginatedConsultants()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        var consultants = await TestDataCreateUser.CreateTestUserList(_dbContext, consultantRole);

        _mockKeyManagementService
            .Setup(kms => kms.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("decryptedKey");
        _mockAesEncryptionService
            .Setup(aes => aes.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("1234567890");

        var handler = new GetAllConsultants.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );
        var query = new GetAllConsultants.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value.Items.Count());

        // Ensure the consultants are sorted by ID in descending order
        var sortedConsultants = result.Value.Items.OrderByDescending(u => u.Id).ToList();
        Assert.Equal(sortedConsultants, result.Value.Items);
    }

    [Fact]
    public async Task Handle_KeywordFilter_ReturnsFilteredResults()
    {
        // Arrange
        var adminRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Admin");
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        var consultants = await TestDataCreateUser.CreateTestUserList(_dbContext, consultantRole);

        _mockKeyManagementService
            .Setup(kms => kms.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("decryptedKey");
        _mockAesEncryptionService
            .Setup(aes => aes.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("1234567890");

        var handler = new GetAllConsultants.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );
        var query = new GetAllConsultants.Query(Keyword: "User One");

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
        var consultantRole = await TestDataCreateUserRole.CreateTestUserRole(
            _dbContext,
            "Consultant"
        );
        var adminUser = await TestDataCreateUser.CreateTestUser(_dbContext, adminRole);
        _currentUser.SetUser(adminUser);

        var consultants = await TestDataCreateUser.CreateTestUserList(_dbContext, consultantRole);

        _mockKeyManagementService
            .Setup(kms => kms.DecryptKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("decryptedKey");
        _mockAesEncryptionService
            .Setup(aes => aes.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("1234567890");

        var handler = new GetAllConsultants.Handler(
            _dbContext,
            _currentUser,
            _mockAesEncryptionService.Object,
            _mockKeyManagementService.Object,
            _encryptionSettings
        );

        // Act
        var query = new GetAllConsultants.Query(PageNumber: 2, PageSize: 2);
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);

        // Ensure the correct consultants are returned for the second page
        Assert.Equal("User One", result.Value.Items.FirstOrDefault()?.Name);
        Assert.Equal("User One", result.Value.Items.LastOrDefault()?.Name);
    }
}
