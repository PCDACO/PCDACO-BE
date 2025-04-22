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
        string ProoUrl,
        bool IsWithdrawalRequest = false,
        WithdrawalRequestDetailsDto? WithdrawalDetails = null
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

        public static Response FromWithdrawalRequest(WithdrawalRequest request) =>
            new(
                request.Id,
                "Withdrawal Request",
                false, // Withdrawal is always an expense
                request.Amount,
                0, // Balance after is not applicable for requests
                $"Withdrawal request to {request.BankAccount.BankInfo.Name}",
                GetTimestampFromUuid.Execute(request.Id),
                request.Status.ToString(),
                new TransactionDetailsDto(
                    null,
                    request.BankAccount.BankInfo.Name,
                    request.BankAccount.BankAccountName
                ),
                string.Empty,
                true,
                new WithdrawalRequestDetailsDto(
                    request.RejectReason,
                    request.ProcessedAt,
                    request.AdminNote,
                    request.TransactionId
                )
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

    public record WithdrawalRequestDetailsDto(
        string RejectReason,
        DateTimeOffset? ProcessedAt,
        string? AdminNote,
        Guid? TransactionId
    );

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

            // Get transactions
            var transactionQuery = context
                .Transactions.Include(t => t.Type)
                .Include(t => t.Booking)
                .Include(t => t.BankAccount)
                .ThenInclude(b => b!.BankInfo)
                .AsQueryable();

            // Get withdrawal requests
            var withdrawalQuery = context
                .WithdrawalRequests.Include(w => w.BankAccount)
                .ThenInclude(b => b.BankInfo)
                .Where(w => w.UserId == currentUser.User!.Id)
                .AsQueryable();

            if (currentUser.User.IsAdmin())
            {
                transactionQuery = transactionQuery.Where(t =>
                    t.Type.Name == TransactionTypeNames.BookingPayment
                    || t.Type.Name == TransactionTypeNames.Withdrawal
                    || t.Type.Name == TransactionTypeNames.Refund
                );
            }
            else
            {
                // For non-admin users, keep existing filter
                transactionQuery = transactionQuery.Where(t =>
                    t.FromUserId == currentUser.User!.Id || t.ToUserId == currentUser.User!.Id
                );
            }

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.TransactionType))
            {
                transactionQuery = transactionQuery.Where(t =>
                    t.Type.Name == request.TransactionType
                );

                // Only show withdrawal requests when specifically filtering for withdrawals
                if (request.TransactionType != TransactionTypeNames.Withdrawal)
                {
                    withdrawalQuery = withdrawalQuery.Where(w => false); // Exclude all
                }
            }

            if (request.FromDate.HasValue)
            {
                var fromId = UuidToolkit.CreateUuidV7FromSpecificDate(request.FromDate.Value);
                transactionQuery = transactionQuery.Where(t => t.Id.CompareTo(fromId) >= 0);
                withdrawalQuery = withdrawalQuery.Where(w => w.Id.CompareTo(fromId) >= 0);
            }

            if (request.ToDate.HasValue)
            {
                var toId = UuidToolkit.CreateUuidV7FromSpecificDate(
                    request.ToDate.Value.AddDays(1)
                );
                transactionQuery = transactionQuery.Where(t => t.Id.CompareTo(toId) < 0);
                withdrawalQuery = withdrawalQuery.Where(w => w.Id.CompareTo(toId) < 0);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                transactionQuery = transactionQuery.Where(t =>
                    EF.Functions.ILike(t.Description, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(t.Type.Name, $"%{request.SearchTerm}%")
                );
                withdrawalQuery = withdrawalQuery.Where(w =>
                    EF.Functions.ILike(w.BankAccount.BankInfo.Name, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(w.BankAccount.BankAccountName, $"%{request.SearchTerm}%")
                );
            }

            // Get total counts
            var totalTransactions = await transactionQuery.CountAsync(cancellationToken);
            var totalWithdrawals = await withdrawalQuery.CountAsync(cancellationToken);
            var totalCount = totalTransactions + totalWithdrawals;

            // Calculate pagination
            var skip = (request.PageNumber - 1) * request.PageSize;
            var take = request.PageSize;

            // Get transactions and withdrawal requests
            var transactions = await transactionQuery
                .OrderByDescending(t => t.Id)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var withdrawals = await withdrawalQuery
                .OrderByDescending(w => w.Id)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Combine and sort results
            var combinedResults = transactions
                .Select(t => Response.FromEntity(t, currentUser.User!.Id))
                .Concat(withdrawals.Select(w => Response.FromWithdrawalRequest(w)))
                .OrderByDescending(r => r.CreatedAt)
                .Take(request.PageSize)
                .ToList();

            var hasNext = (skip + take) < totalCount;

            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    combinedResults,
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
