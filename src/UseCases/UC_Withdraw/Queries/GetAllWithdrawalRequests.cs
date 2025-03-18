using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;
using UUIDNext.Tools;

namespace UseCases.UC_Withdraw.Queries;

public sealed class GetAllWithdrawalRequests
{
    public sealed record Query(
        int Limit,
        Guid? LastId,
        string? SearchTerm = null,
        WithdrawRequestStatusEnum? Status = null,
        DateTimeOffset? FromDate = null,
        DateTimeOffset? ToDate = null
    ) : IRequest<Result<CursorPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        UserDto User,
        BankAccountDto BankAccount,
        decimal Amount,
        string Status,
        DateTimeOffset CreatedAt,
        ProcessedInfoDto? ProcessedInfo
    )
    {
        public static async Task<Response> FromEntity(
            WithdrawalRequest request,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            // Decrypt phone number
            string userDecryptedKey = keyManagementService.DecryptKey(
                request.User.EncryptionKey.EncryptedKey,
                masterKey
            );
            string decryptedPhone = await aesEncryptionService.Decrypt(
                request.User.Phone,
                userDecryptedKey,
                request.User.EncryptionKey.IV
            );

            // Decrypt bank account number
            string bankDecryptedKey = keyManagementService.DecryptKey(
                request.BankAccount.EncryptionKey.EncryptedKey,
                masterKey
            );
            string decryptedAccountNumber = await aesEncryptionService.Decrypt(
                request.BankAccount.EncryptedBankAccount,
                bankDecryptedKey,
                request.BankAccount.EncryptionKey.IV
            );

            return new(
                request.Id,
                new UserDto(
                    request.User.Id,
                    request.User.Name,
                    request.User.Email,
                    decryptedPhone,
                    request.User.Balance
                ),
                new BankAccountDto(
                    request.BankAccount.Id,
                    request.BankAccount.BankInfo.Name,
                    request.BankAccount.BankInfo.Code,
                    request.BankAccount.BankAccountName,
                    decryptedAccountNumber
                ),
                request.Amount,
                request.Status.ToString(),
                GetTimestampFromUuid.Execute(request.Id),
                request.ProcessedAt.HasValue
                    ? new ProcessedInfoDto(
                        request.ProcessedAt.Value,
                        request.ProcessedByAdmin?.Name ?? string.Empty,
                        request.AdminNote ?? string.Empty,
                        request.RejectReason,
                        request.Transaction?.Id
                    )
                    : null
            );
        }
    }

    public record UserDto(Guid Id, string Name, string Email, string Phone, decimal Balance);

    public record BankAccountDto(
        Guid Id,
        string BankName,
        string BankCode,
        string AccountName,
        string AccountNumber
    );

    public record ProcessedInfoDto(
        DateTimeOffset ProcessedAt,
        string ProcessedByAdminName,
        string AdminNote,
        string RejectReason,
        Guid? TransactionId
    );

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Query, Result<CursorPaginatedResponse<Response>>>
    {
        public async Task<Result<CursorPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Chỉ admin mới có quyền truy cập");

            var query = context
                .WithdrawalRequests.Include(w => w.User)
                .ThenInclude(u => u.EncryptionKey)
                .Include(w => w.BankAccount)
                .ThenInclude(b => b.BankInfo)
                .Include(w => w.BankAccount)
                .ThenInclude(b => b.EncryptionKey)
                .Include(w => w.ProcessedByAdmin)
                .Include(w => w.Transaction)
                .AsQueryable();

            // Apply filters
            if (request.Status.HasValue)
            {
                query = query.Where(w => w.Status == request.Status.Value);
            }

            if (request.FromDate.HasValue)
            {
                var fromId = UuidToolkit.CreateUuidV7FromSpecificDate(request.FromDate.Value);
                query = query.Where(w => w.Id.CompareTo(fromId) >= 0);
            }

            if (request.ToDate.HasValue)
            {
                var toId = UuidToolkit.CreateUuidV7FromSpecificDate(
                    request.ToDate.Value.AddDays(1)
                );
                query = query.Where(w => w.Id.CompareTo(toId) < 0);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(w =>
                    EF.Functions.ILike(w.User.Name, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(w.User.Email, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(w.BankAccount.BankAccountName, $"%{request.SearchTerm}%")
                );
            }

            // Apply cursor pagination
            if (request.LastId.HasValue)
            {
                query = query.Where(w => w.Id.CompareTo(request.LastId.Value) < 0);
            }

            // Order by Id descending (newest first since using UUID v7)
            query = query.OrderByDescending(w => w.Id);

            var totalCount = await query.CountAsync(cancellationToken);
            var withdrawals = await query
                .Take(request.Limit)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var responses = new List<Response>();
            foreach (var withdrawal in withdrawals)
            {
                responses.Add(
                    await Response.FromEntity(
                        withdrawal,
                        encryptionSettings.Key,
                        aesEncryptionService,
                        keyManagementService
                    )
                );
            }

            var hasMore = withdrawals.Count == request.Limit;
            var lastId = withdrawals.LastOrDefault()?.Id;

            return Result.Success(
                new CursorPaginatedResponse<Response>(
                    responses,
                    totalCount,
                    request.Limit,
                    lastId,
                    hasMore
                )
            );
        }
    }
}
