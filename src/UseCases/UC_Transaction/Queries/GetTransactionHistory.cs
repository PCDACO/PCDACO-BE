using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;
using UUIDNext.Tools;

namespace UseCases.UC_Transaction.Queries;

public sealed class GetTransactionHistory
{
    public sealed record Query(
        int Limit,
        Guid? LastId,
        string? SearchTerm = null,
        string? TransactionType = null,
        DateTimeOffset? FromDate = null,
        DateTimeOffset? ToDate = null
    ) : IRequest<Result<CursorPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        string Type,
        decimal Amount,
        decimal BalanceAfter,
        string Description,
        DateTimeOffset CreatedAt,
        string Status,
        TransactionDetailsDto Details,
        string ProoUrl
    )
    {
        public static Response FromEntity(Transaction transaction) =>
            new(
                transaction.Id,
                transaction.Type.Name,
                transaction.Amount,
                transaction.BalanceAfter,
                transaction.Description,
                GetTimestampFromUuid.Execute(transaction.Id),
                transaction.Status.ToString(),
                new TransactionDetailsDto(
                    transaction.Booking?.Id,
                    transaction.BankAccount?.BankInfo.Name,
                    transaction.BankAccount?.BankAccountName
                ),
                transaction.ProofUrl
            );
    }

    public record TransactionDetailsDto(Guid? BookingId, string? BankName, string? BankAccountName);

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Query, Result<CursorPaginatedResponse<Response>>>
    {
        public async Task<Result<CursorPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            if (currentUser.User == null)
                return Result.Forbidden("Bạn cần đăng nhập để xem lịch sử giao dịch");

            var query = context
                .Transactions.Include(t => t.Type)
                .Include(t => t.Booking)
                .Include(t => t.BankAccount)
                .ThenInclude(b => b!.BankInfo)
                .Where(t =>
                    t.FromUserId == currentUser.User!.Id || t.ToUserId == currentUser.User!.Id
                )
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.TransactionType))
            {
                query = query.Where(t => t.Type.Name == request.TransactionType);
            }

            if (request.FromDate.HasValue)
            {
                var fromId = UuidToolkit.CreateUuidV7FromSpecificDate(request.FromDate.Value);
                query = query.Where(t => t.Id.CompareTo(fromId) >= 0);
            }

            if (request.ToDate.HasValue)
            {
                var toId = UuidToolkit.CreateUuidV7FromSpecificDate(
                    request.ToDate.Value.AddDays(1)
                ); // Add 1 day to include the entire end date
                query = query.Where(t => t.Id.CompareTo(toId) < 0);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(t =>
                    EF.Functions.ILike(t.Description, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(t.Type.Name, $"%{request.SearchTerm}%")
                );
            }

            // Apply cursor pagination
            if (request.LastId.HasValue)
            {
                query = query.Where(t => t.Id.CompareTo(request.LastId.Value) < 0);
            }

            // Order by Id descending since it contains timestamp
            query = query.OrderByDescending(t => t.Id);

            var totalCount = await query.CountAsync(cancellationToken);
            var transactions = await query
                .Take(request.Limit)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var hasMore = transactions.Count == request.Limit;
            var lastId = transactions.LastOrDefault()?.Id;

            return Result.Success(
                new CursorPaginatedResponse<Response>(
                    transactions.Select(Response.FromEntity),
                    totalCount,
                    request.Limit,
                    lastId,
                    hasMore
                )
            );
        }
    }
}
