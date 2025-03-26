using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_BankAccount.Queries;

public sealed class GetBankAccountById
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid Id,
        Guid BankInfoId,
        string BankName,
        string BankCode,
        string BankShortName,
        string AccountNumber,
        string AccountName,
        bool IsPrimary,
        string BankIconUrl
    )
    {
        public static async Task<Response> FromEntity(
            BankAccount bankAccount,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            string decryptedKey = keyManagementService.DecryptKey(
                bankAccount.EncryptionKey.EncryptedKey,
                masterKey
            );
            string decryptedAccountNumber = await aesEncryptionService.Decrypt(
                bankAccount.EncryptedBankAccount,
                decryptedKey,
                bankAccount.EncryptionKey.IV
            );

            return new(
                Id: bankAccount.Id,
                BankInfoId: bankAccount.BankInfoId,
                BankName: bankAccount.BankInfo.Name,
                BankCode: bankAccount.BankInfo.Code,
                BankShortName: bankAccount.BankInfo.ShortName,
                AccountNumber: decryptedAccountNumber,
                AccountName: bankAccount.BankAccountName,
                IsPrimary: bankAccount.IsPrimary,
                BankIconUrl: bankAccount.BankInfo.IconUrl
            );
        }
    }

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Check if user is driver or owner
            if (!currentUser.User!.IsDriver() && !currentUser.User!.IsOwner())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Get the current user
            User? user = await context
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == currentUser.User!.Id && !x.IsDeleted,
                    cancellationToken
                );

            if (user is null)
                return Result.NotFound(ResponseMessages.UserNotFound);

            // Get the bank account
            BankAccount? bankAccount = await context
                .BankAccounts.AsNoTracking()
                .Include(ba => ba.BankInfo)
                .Include(ba => ba.EncryptionKey)
                .FirstOrDefaultAsync(ba => ba.Id == request.Id && !ba.IsDeleted, cancellationToken);

            if (bankAccount is null)
                return Result.NotFound(ResponseMessages.BankAccountNotFound);

            // Check ownership
            if (bankAccount.UserId != currentUser.User!.Id)
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Map to response
            var response = await Response.FromEntity(
                bankAccount,
                encryptionSettings.Key,
                aesEncryptionService,
                keyManagementService
            );

            return Result.Success(response, ResponseMessages.Fetched);
        }
    }
}
