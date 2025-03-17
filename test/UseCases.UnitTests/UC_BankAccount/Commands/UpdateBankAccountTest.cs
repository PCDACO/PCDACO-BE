using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_BankAccount.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_BankAccount.Commands;

[Collection("Test Collection")]
public class UpdateBankAccountTest(DatabaseTestBase fixture) : IAsyncLifetime
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

        var bankInfo = await CreateTestBankInfo();

        var handler = new UpdateBankAccount.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var command = new UpdateBankAccount.Command(
            Id: Guid.NewGuid(),
            BankInfoId: bankInfo.Id,
            AccountNumber: "1234567890",
            AccountName: "Updated Account",
            IsPrimary: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        // Setup with non-existent user
        var user = await CreateTestUser();
        user.IsDeleted = true;
        await _dbContext.SaveChangesAsync();

        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo();

        var handler = new UpdateBankAccount.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var command = new UpdateBankAccount.Command(
            Id: Guid.NewGuid(),
            BankInfoId: bankInfo.Id,
            AccountNumber: "1234567890",
            AccountName: "Updated Account",
            IsPrimary: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

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

        var bankInfo = await CreateTestBankInfo();

        var handler = new UpdateBankAccount.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        // Use a non-existent bank account ID
        var command = new UpdateBankAccount.Command(
            Id: Guid.NewGuid(), // Non-existent ID
            BankInfoId: bankInfo.Id,
            AccountNumber: "1234567890",
            AccountName: "Updated Account",
            IsPrimary: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.BankAccountNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_BankInfoNotFound_ReturnsNotFound()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo();
        var bankAccount = await CreateTestBankAccount(user.Id, bankInfo.Id, false);

        var handler = new UpdateBankAccount.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var command = new UpdateBankAccount.Command(
            Id: bankAccount.Id,
            BankInfoId: Guid.NewGuid(), // Non-existent bank info ID
            AccountNumber: "9876543210",
            AccountName: "Updated Account",
            IsPrimary: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.BankInfoNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_BankAccountNotOwnedByUser_ReturnsNotFound()
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        var user1 = await TestDataCreateUser.CreateTestUser(_dbContext, userRole, "user1@test.com");
        var user2 = await TestDataCreateUser.CreateTestUser(_dbContext, userRole, "user2@test.com");

        _currentUser.SetUser(user1);

        var bankInfo = await CreateTestBankInfo();
        // Create bank account owned by user2
        var bankAccount = await CreateTestBankAccount(user2.Id, bankInfo.Id, false);

        var handler = new UpdateBankAccount.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var command = new UpdateBankAccount.Command(
            Id: bankAccount.Id,
            BankInfoId: bankInfo.Id,
            AccountNumber: "9876543210",
            AccountName: "Updated Account",
            IsPrimary: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequestWithPrimary_UpdatesExistingPrimaryAccounts()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo();
        var bankInfo2 = await CreateTestBankInfo("Another Bank");

        // Create multiple accounts - one primary, one not
        var primaryAccount = await CreateTestBankAccount(
            user.Id,
            bankInfo.Id,
            true,
            "Primary Account"
        );
        var regularAccount = await CreateTestBankAccount(
            user.Id,
            bankInfo.Id,
            false,
            "Regular Account"
        );

        var handler = new UpdateBankAccount.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var command = new UpdateBankAccount.Command(
            Id: regularAccount.Id,
            BankInfoId: bankInfo2.Id,
            AccountNumber: "9876543210",
            AccountName: "New Primary Account",
            IsPrimary: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(ResponseMessages.Updated, result.SuccessMessage);

        // Verify the old primary account is no longer primary
        var updatedExistingAccount = await _dbContext.BankAccounts.FindAsync(primaryAccount.Id);
        Assert.NotNull(updatedExistingAccount);
        Assert.False(updatedExistingAccount.IsPrimary);

        // Verify the updated account is now primary
        var updatedAccount = await _dbContext.BankAccounts.FindAsync(regularAccount.Id);
        Assert.NotNull(updatedAccount);
        Assert.True(updatedAccount.IsPrimary);
        Assert.Equal("New Primary Account", updatedAccount.BankAccountName);
        Assert.Equal(bankInfo2.Id, updatedAccount.BankInfoId);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesAccountDetails()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo1 = await CreateTestBankInfo();
        var bankInfo2 = await CreateTestBankInfo("Another Bank");
        var bankAccount = await CreateTestBankAccount(user.Id, bankInfo1.Id, false);

        var handler = new UpdateBankAccount.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var command = new UpdateBankAccount.Command(
            Id: bankAccount.Id,
            BankInfoId: bankInfo2.Id,
            AccountNumber: "9876543210",
            AccountName: "Updated Account Name",
            IsPrimary: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify response
        Assert.Equal(bankAccount.Id, result.Value.Id);
        Assert.Equal("Updated Account Name", result.Value.AccountName);
        Assert.Equal(bankInfo2.Name, result.Value.BankName);
        Assert.False(result.Value.IsPrimary);

        // Verify database update
        var updatedAccount = await _dbContext
            .BankAccounts.Include(ba => ba.EncryptionKey)
            .FirstOrDefaultAsync(ba => ba.Id == bankAccount.Id);

        Assert.NotNull(updatedAccount);
        Assert.Equal(bankInfo2.Id, updatedAccount.BankInfoId);
        Assert.Equal("Updated Account Name", updatedAccount.BankAccountName);
        Assert.False(updatedAccount.IsPrimary);
    }

    [Theory]
    [InlineData("", "TestAccount", "Số tài khoản không được để trống")]
    [InlineData("1234", "", "Tên tài khoản không được để trống")]
    [InlineData("", "", "Số tài khoản phải có ít nhất 5 ký tự")]
    [InlineData("1234", "Te", "Tên tài khoản phải có ít nhất 3 ký tự")]
    [InlineData("123", "TestAccount", "Số tài khoản phải có ít nhất 5 ký tự")]
    public void Validator_InvalidRequest_ReturnsValidationErrors(
        string accountNumber,
        string accountName,
        string expectedError
    )
    {
        // Arrange
        var validator = new UpdateBankAccount.Validator();
        var command = new UpdateBankAccount.Command(
            Id: Guid.NewGuid(),
            BankInfoId: Guid.NewGuid(),
            AccountNumber: accountNumber,
            AccountName: accountName,
            IsPrimary: false
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(expectedError, result.Errors.Select(e => e.ErrorMessage));
    }

    private async Task<User> CreateTestUser(string roleName = "Driver")
    {
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        return await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
    }

    private async Task<BankInfo> CreateTestBankInfo(string name = "Test Bank")
    {
        var bankInfo = new BankInfo
        {
            BankLookUpId = Guid.NewGuid(),
            Name = name,
            Code = name.Replace(" ", "").Substring(0, Math.Min(4, name.Length)).ToUpper(),
            Bin = 970425,
            ShortName = name.Replace(" ", "").Substring(0, Math.Min(8, name.Length)),
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
