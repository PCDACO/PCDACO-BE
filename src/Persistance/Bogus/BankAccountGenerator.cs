using Domain.Entities;
using Domain.Shared;
using UseCases.Abstractions;
using UUIDNext;

namespace Persistance.Bogus;

public class BankAccountDummy
{
    public required string BankAccountNumber { get; set; }
    public required string BankAccountName { get; set; }
    public required bool IsPrimary { get; set; }
}

public static class BankAccountGenerator
{
    public static readonly BankAccountDummy[] BankAccounts =
    [
        new()
        {
            BankAccountNumber = "123456789",
            BankAccountName = "NGUYEN VAN A",
            IsPrimary = true
        },
        new()
        {
            BankAccountNumber = "987654321",
            BankAccountName = "NGUYEN VAN A",
            IsPrimary = false
        },
        new()
        {
            BankAccountNumber = "456789123",
            BankAccountName = "TRAN THI B",
            IsPrimary = true
        }
    ];

    public static async Task<BankAccount[]> Execute(
        User[] users,
        BankInfo[] bankInfos,
        EncryptionSettings encryptionSettings,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService
    )
    {
        var bankAccountTasks = BankAccounts.Select(async ba =>
        {
            (string key, string iv) = await keyManagementService.GenerateKeyAsync();
            string encryptedBankAccount = await aesEncryptionService.Encrypt(
                ba.BankAccountNumber,
                key,
                iv
            );
            string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);

            Guid newBankAccountId = Uuid.NewDatabaseFriendly(Database.PostgreSql);
            EncryptionKey encryptionKeyObject = new() { EncryptedKey = encryptedKey, IV = iv };

            return new BankAccount
            {
                Id = newBankAccountId,
                UserId = users[0].Id, // You might want to randomize this
                BankInfoId = bankInfos[0].Id, // You might want to randomize this
                EncryptionKeyId = encryptionKeyObject.Id,
                EncryptedBankAccount = encryptedBankAccount,
                BankAccountName = ba.BankAccountName,
                IsPrimary = ba.IsPrimary,
                EncryptionKey = encryptionKeyObject
            };
        });

        return await Task.WhenAll(bankAccountTasks);
    }
}
