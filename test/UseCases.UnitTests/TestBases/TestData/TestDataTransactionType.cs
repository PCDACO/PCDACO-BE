using Domain.Constants;
using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataTransactionType
{
    private static TransactionType CreateTransactionType(string typeName) =>
        new() { Id = Uuid.NewDatabaseFriendly(Database.PostgreSql), Name = typeName };

    public static async Task<TransactionType> CreateTestTransactionType(
        AppDBContext dBContext,
        string typeName
    )
    {
        var transactionType = CreateTransactionType(typeName);

        await dBContext.TransactionTypes.AddAsync(transactionType);
        await dBContext.SaveChangesAsync();

        return transactionType;
    }

    public static async Task<List<TransactionType>> CreateTestTransactionTypes(
        AppDBContext dBContext,
        List<string> typeNames
    )
    {
        var transactionTypes = typeNames
            .Select(typeName => CreateTransactionType(typeName))
            .ToList();

        await dBContext.TransactionTypes.AddRangeAsync(transactionTypes);
        await dBContext.SaveChangesAsync();

        return transactionTypes;
    }

    public static async Task<List<TransactionType>> InitializeTestTransactionTypes(
        AppDBContext dBContext
    )
    {
        List<string> typeNames =
        [
            TransactionTypeNames.BookingPayment,
            TransactionTypeNames.PlatformFee,
            TransactionTypeNames.OwnerEarning,
            TransactionTypeNames.Withdrawal,
            TransactionTypeNames.Refund,
            TransactionTypeNames.Compensation
        ];

        var transactionTypes = new List<TransactionType>();

        for (var i = 0; i < typeNames.Count; i++)
        {
            var transactionType = CreateTransactionType(typeNames[i]);
            transactionTypes.Add(transactionType);
        }

        await dBContext.TransactionTypes.AddRangeAsync(transactionTypes);
        await dBContext.SaveChangesAsync();

        return transactionTypes;
    }
}
