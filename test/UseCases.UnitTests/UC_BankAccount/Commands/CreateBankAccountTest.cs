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
using UUIDNext;

namespace UseCases.UnitTests.UC_BankAccount.Commands;

[Collection("Test Collection")]
public class CreateBankAccountTest(DatabaseTestBase fixture) : IAsyncLifetime
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
    public async Task Handle_UserNotDriverAndOwner_ReturnsForbidden(string roleName)
    {
        // Arrange
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, roleName);
        var user = await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo();

        var handler = new CreateBankAccount.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var command = new CreateBankAccount.Command(
            BankInfoId: bankInfo.Id,
            AccountNumber: "1234567890",
            AccountName: "Test Account",
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

        var handler = new CreateBankAccount.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var command = new CreateBankAccount.Command(
            BankInfoId: bankInfo.Id,
            AccountNumber: "1234567890",
            AccountName: "Test Account",
            IsPrimary: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.UserNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_BankInfoNotFound_ReturnsNotFound()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var handler = new CreateBankAccount.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var command = new CreateBankAccount.Command(
            BankInfoId: Guid.NewGuid(),
            AccountNumber: "1234567890",
            AccountName: "Test Account",
            IsPrimary: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.BankInfoNotFound, result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequestWithPrimary_UpdatesExistingPrimaryAccounts()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo();

        // Create an existing primary account
        var existingAccount = await CreateTestBankAccount(user.Id, bankInfo.Id, true);

        var handler = new CreateBankAccount.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var command = new CreateBankAccount.Command(
            BankInfoId: bankInfo.Id,
            AccountNumber: "9876543210",
            AccountName: "New Primary Account",
            IsPrimary: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Contains(ResponseMessages.Created, result.SuccessMessage);

        // Verify the old primary account is no longer primary
        var updatedExistingAccount = await _dbContext.BankAccounts.FindAsync(existingAccount.Id);
        Assert.NotNull(updatedExistingAccount);
        Assert.False(updatedExistingAccount.IsPrimary);

        // Verify the new account is primary
        var newAccount = await _dbContext.BankAccounts.FirstOrDefaultAsync(a =>
            a.BankAccountName == "New Primary Account"
        );
        Assert.NotNull(newAccount);
        Assert.True(newAccount.IsPrimary);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesEncryptedBankAccount()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo();

        var handler = new CreateBankAccount.Handler(
            _dbContext,
            _aesService,
            _keyService,
            _encryptionSettings,
            _currentUser
        );

        var command = new CreateBankAccount.Command(
            BankInfoId: bankInfo.Id,
            AccountNumber: "1234567890",
            AccountName: "Test Account",
            IsPrimary: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);

        // Verify response structure
        Assert.NotNull(result.Value);
        Assert.Equal(bankInfo.Name, result.Value.BankName);
        Assert.Equal("Test Account", result.Value.AccountName);
        Assert.False(result.Value.IsPrimary);

        // Verify the bank account was created
        var createdAccount = await _dbContext
            .BankAccounts.Include(ba => ba.EncryptionKey)
            .FirstOrDefaultAsync(ba => ba.BankAccountName == "Test Account");

        Assert.NotNull(createdAccount);
        Assert.Equal(user.Id, createdAccount.UserId);
        Assert.Equal(bankInfo.Id, createdAccount.BankInfoId);
        Assert.NotNull(createdAccount.EncryptedBankAccount);
        Assert.NotNull(createdAccount.EncryptionKey);
        Assert.NotNull(createdAccount.EncryptionKey.EncryptedKey);
        Assert.NotNull(createdAccount.EncryptionKey.IV);
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
        var validator = new CreateBankAccount.Validator();
        var command = new CreateBankAccount.Command(
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

    private async Task<User> CreateTestUser()
    {
        var userRole = await TestDataCreateUserRole.CreateTestUserRole(_dbContext, "Driver");
        return await TestDataCreateUser.CreateTestUser(_dbContext, userRole);
    }

    private async Task<BankInfo> CreateTestBankInfo()
    {
        var bankInfo = new BankInfo
        {
            BankLookUpId = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = "Ngân hàng TMCP An Bình",
            Code = "ABB",
            Bin = 970425,
            ShortName = "ABBANK",
            LogoUrl = "https://api.vietqr.io/img/ABB.png",
            IconUrl = "https://cdn.banklookup.net/assets/images/bank-icons/ABB.svg",
            SwiftCode = "ABBKVNVX",
            LookupSupported = 1,
        };

        await _dbContext.BankInfos.AddAsync(bankInfo);
        await _dbContext.SaveChangesAsync();

        return bankInfo;
    }

    private async Task<BankAccount> CreateTestBankAccount(
        Guid userId,
        Guid bankInfoId,
        bool isPrimary
    )
    {
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
            BankAccountName = "Existing Account",
            IsPrimary = isPrimary,
        };

        await _dbContext.BankAccounts.AddAsync(bankAccount);
        await _dbContext.SaveChangesAsync();

        return bankAccount;
    }
}
