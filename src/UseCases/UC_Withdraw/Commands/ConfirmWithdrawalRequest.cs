using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Withdraw.Commands;

public sealed class ConfirmWithdrawalRequest
{
    public record Command(
        Guid WithdrawalRequestId,
        Stream TransactionProofImage,
        string? AdminNote = null
    ) : IRequest<Result<Response>>;

    public record Response(
        Guid WithdrawalId,
        Guid TransactionId,
        string Status,
        string TransactionProofUrl
    );

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        ICloudinaryServices cloudinaryServices
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Chỉ admin mới có quyền xác nhận giao dịch");

            // Get withdrawal request with necessary relations
            var withdrawal = await context
                .WithdrawalRequests.Include(w => w.User)
                .Include(w => w.BankAccount)
                .FirstOrDefaultAsync(w => w.Id == request.WithdrawalRequestId, cancellationToken);

            if (withdrawal == null)
                return Result.NotFound("Không tìm thấy yêu cầu rút tiền");

            if (withdrawal.Status != WithdrawRequestStatusEnum.Pending)
                return Result.Error("Yêu cầu rút tiền này không ở trạng thái chờ xử lý");

            // Get withdrawal transaction type
            var withdrawalType = await context.TransactionTypes.FirstOrDefaultAsync(
                t => t.Name == TransactionTypeNames.Withdrawal,
                cancellationToken
            );

            if (withdrawalType == null)
                return Result.Error("Không tìm thấy loại giao dịch rút tiền");

            // Upload transaction proof
            string proofUrl = await cloudinaryServices.UploadTransactionProofAsync(
                $"Withdrawal-{withdrawal.Id}-Proof",
                request.TransactionProofImage,
                cancellationToken
            );

            // Create transaction record
            var transaction = new Transaction
            {
                FromUserId = withdrawal.UserId, // Money moves from user's balance
                ToUserId = withdrawal.UserId, // To their bank account
                BookingId = null,
                BankAccountId = withdrawal.BankAccountId,
                TypeId = withdrawalType.Id,
                Status = TransactionStatusEnum.Completed,
                Amount = withdrawal.Amount,
                Description = $"Free driver rut tien",
                BalanceAfter = withdrawal.User.Balance - withdrawal.Amount,
                ProofUrl = proofUrl
            };

            // Update withdrawal request
            withdrawal.Status = WithdrawRequestStatusEnum.Completed;
            withdrawal.ProcessedAt = DateTimeOffset.UtcNow;
            withdrawal.ProcessedByAdminId = currentUser.User.Id;
            withdrawal.AdminNote = request.AdminNote;
            withdrawal.TransactionId = transaction.Id;

            // Update user balance
            withdrawal.User.Balance -= withdrawal.Amount;

            // Save changes
            await context.Transactions.AddAsync(transaction, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new Response(withdrawal.Id, transaction.Id, withdrawal.Status.ToString(), proofUrl),
                ResponseMessages.Updated
            );
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WithdrawalRequestId)
                .NotEmpty()
                .WithMessage("ID yêu cầu rút tiền không được để trống");

            RuleFor(x => x.TransactionProofImage)
                .NotNull()
                .WithMessage("Phải có ảnh chứng minh giao dịch");
        }
    }
}
