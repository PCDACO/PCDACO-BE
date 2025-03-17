using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_BankAccount.Commands;

public sealed class DeleteBankAccount
{
    public record Command(Guid Id) : IRequest<Result>;

    public sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check if user is not driver or owner
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

            // Get the bank account to delete
            BankAccount? bankAccount = await context.BankAccounts.FirstOrDefaultAsync(
                ba => ba.Id == request.Id && ba.UserId == currentUser.User!.Id && !ba.IsDeleted,
                cancellationToken
            );

            if (bankAccount is null)
                return Result.NotFound(ResponseMessages.BankAccountNotFound);

            // Check if bank account has associated transactions
            bool hasTransactions = await context.Transactions.AnyAsync(
                t => t.BankAccountId == bankAccount.Id && !t.IsDeleted,
                cancellationToken
            );

            if (hasTransactions)
                return Result.Error(
                    "Không thể xóa tài khoản ngân hàng đã được sử dụng trong giao dịch"
                );

            // Check for withdrawal requests
            bool hasWithdrawals = await context.WithdrawalRequests.AnyAsync(
                w => w.BankAccountId == bankAccount.Id && !w.IsDeleted,
                cancellationToken
            );

            if (hasWithdrawals)
                return Result.Error("Không thể xóa tài khoản ngân hàng có yêu cầu rút tiền");

            // Soft delete the bank account
            bankAccount.Delete();

            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage(ResponseMessages.Deleted);
        }
    }
}
