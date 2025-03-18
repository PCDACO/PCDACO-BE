using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.UC_BankAccount.Commands;
using UseCases.UnitTests.TestBases;
using UseCases.UnitTests.TestBases.TestData;

namespace UseCases.UnitTests.UC_BankAccount.Commands;

[Collection("Test Collection")]
public class DeleteBankAccountTest(DatabaseTestBase fixture) : IAsyncLifetime
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

        var handler = new DeleteBankAccount.Handler(_dbContext, _currentUser);

        var command = new DeleteBankAccount.Command(Id: Guid.NewGuid());

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

        var handler = new DeleteBankAccount.Handler(_dbContext, _currentUser);

        var command = new DeleteBankAccount.Command(Id: Guid.NewGuid());

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

        var handler = new DeleteBankAccount.Handler(_dbContext, _currentUser);

        // Use a non-existent bank account ID
        var command = new DeleteBankAccount.Command(Id: Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(ResponseMessages.BankAccountNotFound, result.Errors);
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

        var handler = new DeleteBankAccount.Handler(_dbContext, _currentUser);

        var command = new DeleteBankAccount.Command(Id: bankAccount.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Contains(ResponseMessages.ForbiddenAudit, result.Errors);
    }

    [Fact]
    public async Task Handle_BankAccountHasTransactions_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo();
        var bankAccount = await CreateTestBankAccount(user.Id, bankInfo.Id, false);

        // Create transaction type
        var transactionType = new TransactionType { Name = "Deposit" };
        await _dbContext.TransactionTypes.AddAsync(transactionType);
        await _dbContext.SaveChangesAsync();

        //create test car
        var car = await CreateTestCar(user.Id, CarStatusEnum.Available);

        // Create a booking for the transaction
        var booking = new Booking
        {
            UserId = user.Id,
            CarId = car.Id,
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            EndTime = DateTimeOffset.UtcNow.AddHours(3),
            ActualReturnTime = DateTimeOffset.UtcNow.AddHours(3),
            BasePrice = 100000m,
            PlatformFee = 10000m,
            ExcessDay = 0,
            ExcessDayFee = 0,
            TotalAmount = 110000m,
            Note = "Test booking",
            Status = BookingStatusEnum.Completed,
        };
        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Create an associated transaction
        var transaction = new Transaction
        {
            BankAccountId = bankAccount.Id,
            Amount = 100000,
            TypeId = transactionType.Id,
            Status = TransactionStatusEnum.Completed,
            BookingId = booking.Id,
            FromUserId = user.Id,
            ToUserId = user.Id,
        };

        await _dbContext.Transactions.AddAsync(transaction);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteBankAccount.Handler(_dbContext, _currentUser);

        var command = new DeleteBankAccount.Command(Id: bankAccount.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains(
            "Không thể xóa tài khoản ngân hàng đã được sử dụng trong giao dịch",
            result.Errors
        );
    }

    [Fact]
    public async Task Handle_BankAccountHasWithdrawals_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo();
        var bankAccount = await CreateTestBankAccount(user.Id, bankInfo.Id, false);

        // Create an associated withdrawal request
        var withdrawal = new WithdrawalRequest
        {
            UserId = user.Id,
            BankAccountId = bankAccount.Id,
            Amount = 100000,
            Status = WithdrawRequestStatusEnum.Pending,
        };

        await _dbContext.WithdrawalRequests.AddAsync(withdrawal);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteBankAccount.Handler(_dbContext, _currentUser);

        var command = new DeleteBankAccount.Command(Id: bankAccount.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Contains("Không thể xóa tài khoản ngân hàng có yêu cầu rút tiền", result.Errors);
    }

    [Fact]
    public async Task Handle_ValidRequest_DeletesBankAccount()
    {
        // Arrange
        var user = await CreateTestUser();
        _currentUser.SetUser(user);

        var bankInfo = await CreateTestBankInfo();
        var bankAccount = await CreateTestBankAccount(user.Id, bankInfo.Id, false);

        var handler = new DeleteBankAccount.Handler(_dbContext, _currentUser);

        var command = new DeleteBankAccount.Command(Id: bankAccount.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(ResponseMessages.Deleted, result.SuccessMessage);

        // Verify the bank account was soft-deleted
        var deletedAccount = await _dbContext.BankAccounts.FindAsync(bankAccount.Id);
        Assert.NotNull(deletedAccount);
        Assert.True(deletedAccount.IsDeleted);
        Assert.NotNull(deletedAccount.DeletedAt);
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

    private async Task<Car> CreateTestCar(Guid ownerId, CarStatusEnum status)
    {
        var manufacturer = await TestDataCreateManufacturer.CreateTestManufacturer(_dbContext);
        var model = await TestDataCreateModel.CreateTestModel(_dbContext, manufacturer.Id);
        var transmissionType = await TestDataTransmissionType.CreateTestTransmissionType(
            _dbContext,
            "Automatic"
        );
        var fuelType = await TestDataFuelType.CreateTestFuelType(_dbContext, "Electric");

        return await TestDataCreateCar.CreateTestCar(
            _dbContext,
            ownerId,
            model.Id,
            transmissionType,
            fuelType,
            status
        );
    }
}
