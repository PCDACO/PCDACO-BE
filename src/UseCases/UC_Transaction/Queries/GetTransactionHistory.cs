using Ardalis.Result;
using Domain.Constants;
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
        int PageNumber = 1,
        int PageSize = 10,
        string? SearchTerm = null,
        string? TransactionType = null,
        DateTimeOffset? FromDate = null,
        DateTimeOffset? ToDate = null
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        string Type,
        bool IsIncome,
        decimal Amount,
        decimal BalanceAfter,
        string Description,
        DateTimeOffset CreatedAt,
        string Status,
        TransactionDetailsDto Details,
        string ProoUrl
    )
    {
        public static Response FromEntity(Transaction transaction, Guid currentUserId) =>
            new(
                transaction.Id,
                transaction.Type.Name,
                DetermineIsIncome(transaction, currentUserId),
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

        private static bool DetermineIsIncome(Transaction transaction, Guid currentUserId)
        {
            return transaction.Type.Name switch
            {
                TransactionTypeNames.Refund => transaction.ToUserId == currentUserId, // Income for driver, expense for owner/admin
                TransactionTypeNames.BookingPayment => transaction.ToUserId == currentUserId, // Income for owner, expense for driver
                TransactionTypeNames.Withdrawal => false, // Always expense (minus sign) for everyone
                TransactionTypeNames.OwnerEarning => transaction.ToUserId == currentUserId, // Income for owner
                _ => transaction.ToUserId == currentUserId, // Default case: if user is receiving money (ToUserId) then it's income
            };
        }
    }

    public record TransactionDetailsDto(Guid? BookingId, string? BankName, string? BankAccountName);

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
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
                );
                query = query.Where(t => t.Id.CompareTo(toId) < 0);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(t =>
                    EF.Functions.ILike(t.Description, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(t.Type.Name, $"%{request.SearchTerm}%")
                );
            }

            // Order by Id descending (newest first since using UUID v7)
            query = query.OrderByDescending(t => t.Id);

            var totalCount = await query.CountAsync(cancellationToken);

            var transactions = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var hasNext = await query
                .Skip(request.PageSize * request.PageNumber)
                .AnyAsync(cancellationToken: cancellationToken);

            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    transactions.Select(t => Response.FromEntity(t, currentUser.User!.Id)),
                    totalCount,
                    request.PageSize,
                    request.PageNumber,
                    hasNext
                ),
                "Lấy lịch sử giao dịch thành công"
            );
        }
    }
}
