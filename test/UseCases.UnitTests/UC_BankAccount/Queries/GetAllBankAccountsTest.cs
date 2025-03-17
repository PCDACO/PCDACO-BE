using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_BankAccount.Queries;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_BankAccount.Queries;

[Collection("Test Collection")]
public class GetAllBankAccountsTest(DatabaseTestBase fixture) : IAsyncLifetime
{
    private readonly AppDBContext _dbContext = fixture.DbContext;
    private readonly CurrentUser _currentUser = fixture.CurrentUser;
    private readonly IAesEncryptionService _aesService = fixture.AesEncryptionService;
    private readonly IKeyManagementService _keyService = fixture.KeyManagementService;
    private readonly EncryptionSettings _encryptionSettings = fixture.EncryptionSettings;
    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    [Theory]
    [InlineData("Admin")]
    [InlineData("Consultant")]
    [InlineData("Technician")]
    public async Task Handle_UserNotDriverOrOwner_ReturnsForbidden(string roleName)
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);

        var handler = new GetAllBankAccounts.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetAllBankAccounts.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var user = await CreateTestUser();
        user.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        _currentUser.SetUser(user);

        var handler = new GetAllBankAccounts.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetAllBankAccounts.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.UserNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_NoBankAccounts_ReturnsEmptyList()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var handler = new GetAllBankAccounts.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetAllBankAccounts.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
    }

    [Fact]
    public async Task Handle_WithBankAccounts_ReturnsCorrectAccounts()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo1 = await CreateTestBankInfo("Test Bank 1");
        var bankInfo2 = await CreateTestBankInfo("Test Bank 2");

        // Create multiple bank accounts for the user
        var account1 = await CreateTestBankAccount(user.Id, bankInfo1.Id, true, "Primary Account");
        var account2 = await CreateTestBankAccount(
            user.Id,
            bankInfo2.Id,
            false,
            "Secondary Account"
        );

        var handler = new GetAllBankAccounts.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetAllBankAccounts.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(2, result.Value.Items.Count());
        Assert.Equal(2, result.Value.TotalItems);

        // Primary account should be first due to ordering
        var firstAccount = result.Value.Items.First();
        Assert.Equal("Primary Account", firstAccount.AccountName);
        Assert.Equal(bankInfo1.Name, firstAccount.BankName);
        Assert.Equal(bankInfo1.Code, firstAccount.BankCode);
        Assert.True(firstAccount.IsPrimary);
        Assert.Equal("1234567890", firstAccount.AccountNumber); // This is the decrypted account number
    }

    [Fact]
    public async Task Handle_WithKeyword_FiltersByBankNameOrAccountName()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo1 = await CreateTestBankInfo("ABC Bank");
        var bankInfo2 = await CreateTestBankInfo("XYZ Bank");

        await CreateTestBankAccount(user.Id, bankInfo1.Id, false, "Regular Account");
        await CreateTestBankAccount(user.Id, bankInfo2.Id, true, "Special Account");

        var handler = new GetAllBankAccounts.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        // Search by bank name
        var query1 = new GetAllBankAccounts.Query(Keyword: "ABC");

        // Act
        var result1 = await handler.Handle(query1, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result1.Status);
        Assert.Single(result1.Value.Items);
        Assert.Equal("ABC Bank", result1.Value.Items.First().BankName);

        // Search by account name
        var query2 = new GetAllBankAccounts.Query(Keyword: "Special");

        // Act
        var result2 = await handler.Handle(query2, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result2.Status);
        Assert.Single(result2.Value.Items);
        Assert.Equal("Special Account", result2.Value.Items.First().AccountName);
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo("Test Bank");

        // Create 5 bank accounts
        for (int i = 1; i <= 5; i++)
        {
            await CreateTestBankAccount(
                user.Id,
                bankInfo.Id,
                i == 1, // First one is primary
                $"Account {i}"
            );
        }

        var handler = new GetAllBankAccounts.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        // Request first page with 2 items
        var query1 = new GetAllBankAccounts.Query(PageNumber: 1, PageSize: 2);

        // Act
        var result1 = await handler.Handle(query1, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result1.Status);
        Assert.Equal(2, result1.Value.Items.Count());
        Assert.Equal(5, result1.Value.TotalItems);
        Assert.True(result1.Value.HasNext);
        Assert.Equal(1, result1.Value.PageNumber);
        Assert.Equal(2, result1.Value.PageSize);

        // Request second page
        var query2 = new GetAllBankAccounts.Query(PageNumber: 2, PageSize: 2);

        // Act
        var result2 = await handler.Handle(query2, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result2.Status);
        Assert.Equal(2, result2.Value.Items.Count());
        Assert.Equal(5, result2.Value.TotalItems);
        Assert.True(result2.Value.HasNext);
        Assert.Equal(2, result2.Value.PageNumber);

        // Request last page
        var query3 = new GetAllBankAccounts.Query(PageNumber: 3, PageSize: 2);

        // Act
        var result3 = await handler.Handle(query3, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result3.Status);
        Assert.Single(result3.Value.Items);
        Assert.Equal(5, result3.Value.TotalItems);
        Assert.False(result3.Value.HasNext);
        Assert.Equal(3, result3.Value.PageNumber);
    }

    [Fact]
    public async Task Handle_OtherUsersBankAccounts_NotReturned()
    {
        // Arrange
        var user1 = await CreateTestUser();
        var user2 = await CreateTestUser("user2@example.com");
        _currentUser.SetUser(user1);

        var bankInfo = await CreateTestBankInfo();

        // Create account for user1
        await CreateTestBankAccount(user1.Id, bankInfo.Id, true, "User1 Account");

        // Create account for user2
        await CreateTestBankAccount(user2.Id, bankInfo.Id, true, "User2 Account");

        var handler = new GetAllBankAccounts.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetAllBankAccounts.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal("User1 Account", result.Value.Items.First().AccountName);
    }

    [Fact]
    public async Task Handle_DeletedBankAccounts_NotReturned()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo();

        // Create active account
        await CreateTestBankAccount(user.Id, bankInfo.Id, true, "Active Account");

        // Create account and then delete it
        var deletedAccount = await CreateTestBankAccount(
            user.Id,
            bankInfo.Id,
            false,
            "To Be Deleted"
        );
        deletedAccount.Delete();
        await _dbContext.SaveChangesAsync();

        var handler = new GetAllBankAccounts.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetAllBankAccounts.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal("Active Account", result.Value.Items.First().AccountName);
    }

    private async Task<User> CreateTestUser(string email = "test@example.com")
    {
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        return await TestDataCreateUser.CreateTestUser(_dbContext, userRole, email);
    }

    private async Task<BankInfo> CreateTestBankInfo(string name = "Test Bank")
    {
        var bankInfo = new BankInfo
        {
            BankLookUpId = Guid.NewGuid(),
            Name = name,
            Code = name.ToUpper()[..Math.Min(4, name.Length)].Trim(),
            Bin = 970425,
            ShortName = name[..Math.Min(8, name.Length)].Trim(),
            LogoUrl = $"https://api.vietqr.io/img/{name}.png",
            IconUrl = $"https://cdn.banklookup.net/assets/images/bank-icons/{name}.svg",
            SwiftCode = $"{name.Replace(" ", "").Substring(0, Math.Min(4, name.Length))}VNVX",
            LookupSupported = 1,
        };

        await _dbContext.BankInfos.AddAsync(bankInfo);
        await _dbContext.SaveChangesAsync();

        return bankInfo;
    }

    private async Task<BankAccount> CreateTestBankAccount(
        Guid userId,
        Guid bankInfoId,
        bool isPrimary,
        string accountName = "Test Account"
    )
    {
        // Create encryption key
        (string key, string iv) = await _keyService.GenerateKeyAsync();
        string encryptedAccountNumber = await _aesService.Encrypt("1234567890", key, iv);
        string encryptedKey = _keyService.EncryptKey(key, _encryptionSettings.Key);

        var encryptionKey = new EncryptionKey { EncryptedKey = encryptedKey, IV = iv };

        await _dbContext.EncryptionKeys.AddAsync(encryptionKey);
        await _dbContext.SaveChangesAsync();

        var bankAccount = new BankAccount
        {
            UserId = userId,
            BankInfoId = bankInfoId,
            EncryptionKeyId = encryptionKey.Id,
            EncryptedBankAccount = encryptedAccountNumber,
            BankAccountName = accountName,
            IsPrimary = isPrimary,
        };

        await _dbContext.BankAccounts.AddAsync(bankAccount);
        await _dbContext.SaveChangesAsync();

        return bankAccount;
    }
}
