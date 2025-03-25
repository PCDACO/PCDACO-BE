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
        int PageNumber = 1,
        int PageSize = 10,
        string? SearchTerm = "",
        WithdrawRequestStatusEnum? Status = null,
        DateTimeOffset? FromDate = null,
        DateTimeOffset? ToDate = null
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

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
                    request.User.Balance,
                    request.User.AvatarUrl
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

    public record UserDto(
        Guid Id,
        string Name,
        string Email,
        string Phone,
        decimal Balance,
        string AvatarUrl
    );

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
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
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

            // Order by Id descending (newest first since using UUID v7)
            query = query.OrderByDescending(w => w.Id);

            var totalCount = await query.CountAsync(cancellationToken);

            var withdrawalTasks = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .AsNoTracking()
                .Select(withdrawal =>
                    Response.FromEntity(
                        withdrawal,
                        encryptionSettings.Key,
                        aesEncryptionService,
                        keyManagementService
                    )
                )
                .ToListAsync(cancellationToken);

            var withdrawals = await Task.WhenAll(withdrawalTasks);

            var hasNext = await query
                .Skip(request.PageSize * request.PageNumber)
                .AnyAsync(cancellationToken: cancellationToken);

            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    withdrawals,
                    totalCount,
                    request.PageSize,
                    request.PageNumber,
                    hasNext
                ),
                "Lấy danh sách yêu cầu rút tiền thành công"
            );
        }
    }
}
