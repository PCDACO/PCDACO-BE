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
public class GetBankAccountByIdTest(DatabaseTestBase fixture) : IAsyncLifetime
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

        var handler = new GetBankAccountById.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetBankAccountById.Query(Id: Guid.NewGuid());

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

        var handler = new GetBankAccountById.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetBankAccountById.Query(Id: Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.UserNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_BankAccountNotFound_ReturnsNotFound()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var handler = new GetBankAccountById.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetBankAccountById.Query(Id: Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.BankAccountNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_BankAccountNotOwnedByCurrentUser_ReturnsForbidden()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user1 = await TestDataCreateUser.CreateTestUser(_dbContext, userRole, "user1@test.com");
        var user2 = await TestDataCreateUser.CreateTestUser(_dbContext, userRole, "user2@test.com");
        _currentUser.SetUser(user1);

        var bankInfo = await CreateTestBankInfo();
        // Create bank account owned by user2
        var bankAccount = await CreateTestBankAccount(user2.Id, bankInfo.Id, false);

        var handler = new GetBankAccountById.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetBankAccountById.Query(Id: bankAccount.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsCorrectBankAccount()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo("Test Bank ABC");
        var bankAccount = await CreateTestBankAccount(
            user.Id,
            bankInfo.Id,
            true,
            "My Primary Account"
        );

        var handler = new GetBankAccountById.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetBankAccountById.Query(Id: bankAccount.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(ResponseMessages.Fetched, result.SuccessMessage);

        // Verify response data
        Assert.NotNull(result.Value);
        Assert.Equal(bankAccount.Id, result.Value.Id);
        Assert.Equal(bankInfo.Id, result.Value.BankInfoId);
        Assert.Equal(bankInfo.Name, result.Value.BankName);
        Assert.Equal(bankInfo.Code, result.Value.BankCode);
        Assert.Equal("1234567890", result.Value.AccountNumber); // The decrypted account number
        Assert.Equal("My Primary Account", result.Value.AccountName);
        Assert.True(result.Value.IsPrimary);
        Assert.Equal(bankInfo.LogoUrl, result.Value.BankLogoUrl);
    }

    [Fact]
    public async Task Handle_DeletedBankAccount_ReturnsNotFound()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo();
        var bankAccount = await CreateTestBankAccount(user.Id, bankInfo.Id, false);

        // Delete the bank account
        bankAccount.Delete();
        await _dbContext.SaveChangesAsync();

        var handler = new GetBankAccountById.Handler(
            _dbContext,
            _currentUser,
            _aesService,
            _keyService,
            _encryptionSettings
        );

        var query = new GetBankAccountById.Query(Id: bankAccount.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.BankAccountNotFound, result.Errors);
    }

    private async Task<User> CreateTestUser(
        string roleName = "Driver",
        string email = "test@example.com"
    )
    {
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        return await TestDataCreateUser.CreateTestUser(_dbContext, userRole, email);
    }

    private async Task<BankInfo> CreateTestBankInfo(string name = "Test Bank")
    {
        var bankInfo = new BankInfo
        {
            BankLookUpId = Guid.NewGuid(),
            Name = name,
            Code = name[..Math.Min(4, name.Length)].ToUpper().Trim(),
            Bin = 970425,
            ShortName = name.Substring(0, Math.Min(8, name.Length)).Trim(),
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
