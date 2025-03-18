using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
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
        string WithdrawalCode,
        UserDto User,
        BankAccountDto BankAccount,
        decimal Amount,
        string Status,
        DateTimeOffset CreatedAt,
        ProcessedInfoDto? ProcessedInfo
    )
    {
        public static Response FromEntity(WithdrawalRequest request) =>
            new(
                request.Id,
                request.WithdrawalCode,
                new UserDto(
                    request.User.Id,
                    request.User.Name,
                    request.User.Email,
                    request.User.Phone,
                    request.User.Balance
                ),
                new BankAccountDto(
                    request.BankAccount.Id,
                    request.BankAccount.BankInfo.Name,
                    request.BankAccount.BankAccountName
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

    public record UserDto(Guid Id, string Name, string Email, string Phone, decimal Balance);

    public record BankAccountDto(Guid Id, string BankName, string AccountName);

    public record ProcessedInfoDto(
        DateTimeOffset ProcessedAt,
        string ProcessedByAdminName,
        string AdminNote,
        string RejectReason,
        Guid? TransactionId
    );

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Query, Result<CursorPaginatedResponse<Response>>>
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
                .Include(w => w.BankAccount)
                .ThenInclude(b => b.BankInfo)
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
                    EF.Functions.ILike(w.WithdrawalCode, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(w.User.Name, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(w.User.Email, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(w.User.Phone, $"%{request.SearchTerm}%")
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

            var hasMore = withdrawals.Count == request.Limit;
            var lastId = withdrawals.LastOrDefault()?.Id;

            return Result.Success(
                new CursorPaginatedResponse<Response>(
                    withdrawals.Select(Response.FromEntity),
                    totalCount,
                    request.Limit,
                    lastId,
                    hasMore
                )
            );
        }
    }
}
