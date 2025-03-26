using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_BankAccount.Queries;

public sealed class GetAllBankAccounts
{
    public sealed record Query(int PageNumber = 1, int PageSize = 10, string Keyword = "")
        : IRequest<Result<OffsetPaginatedResponse<Response>>>;

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
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
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

            // Query bank accounts
            var query = context
                .BankAccounts.AsNoTracking()
                .Include(ba => ba.BankInfo)
                .Include(ba => ba.EncryptionKey)
                .Where(ba => ba.UserId == currentUser.User!.Id && !ba.IsDeleted)
                .AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                query = query.Where(ba =>
                    EF.Functions.ILike(ba.BankAccountName, $"%{request.Keyword}%")
                    || EF.Functions.ILike(ba.BankInfo.Name, $"%{request.Keyword}%")
                    || EF.Functions.ILike(ba.BankInfo.Code, $"%{request.Keyword}%")
                );
            }

            // Get total count for pagination
            int totalItems = await query.CountAsync(cancellationToken);

            // Get paginated bank accounts
            var bankAccounts = await query
                .OrderByDescending(ba => ba.IsPrimary)
                .ThenByDescending(ba => ba.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Map to response
            var responses = await Task.WhenAll(
                bankAccounts.Select(async ba =>
                    await Response.FromEntity(
                        ba,
                        encryptionSettings.Key,
                        aesEncryptionService,
                        keyManagementService
                    )
                )
            );

            // Check if there are more pages
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();

            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    responses,
                    totalItems,
                    request.PageNumber,
                    request.PageSize,
                    hasNext
                ),
                ResponseMessages.Fetched
            );
        }
    }
}
