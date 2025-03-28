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

public sealed class CreateWithdrawalRequest
{
    public record Command(Guid BankAccountId, decimal Amount) : IRequest<Result<Response>>;

    public record Response(Guid Id)
    {
        public static Response FromEntity(WithdrawalRequest request) => new(request.Id);
    };

    public class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (currentUser.User == null)
                return Result.Forbidden("Bạn cần đăng nhập để thực hiện giao dịch");

            // Verify bank account belongs to user
            var bankAccount = await context.BankAccounts.FirstOrDefaultAsync(
                b => b.Id == request.BankAccountId && b.UserId == currentUser.User!.Id,
                cancellationToken
            );

            if (bankAccount == null)
                return Result.NotFound("Không tìm thấy tài khoản ngân hàng");

            decimal availableBalance = currentUser.User!.Balance - currentUser.User.LockedBalance;

            if (availableBalance < request.Amount)
                return Result.Error(
                    $"Số dư khả dụng không đủ. Số dư khả dụng: {availableBalance:N0} VND, Số dư bị khóa: {currentUser.User.LockedBalance:N0} VND, Số tiền cần rút: {request.Amount:N0} VND"
                );

            var withdrawRequest = await context.WithdrawalRequests.FirstOrDefaultAsync(
                w =>
                    w.UserId == currentUser.User.Id
                    && w.Status == WithdrawRequestStatusEnum.Pending,
                cancellationToken
            );

            if (withdrawRequest != null)
                return Result.Error("Bạn đã có yêu cầu rút tiền đang chờ xử lý");

            var withdrawalRequest = new WithdrawalRequest
            {
                UserId = currentUser.User.Id,
                BankAccountId = request.BankAccountId,
                Amount = request.Amount,
                Status = WithdrawRequestStatusEnum.Pending,
            };

            await context.WithdrawalRequests.AddAsync(withdrawalRequest, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(withdrawalRequest), ResponseMessages.Created);
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BankAccountId)
                .NotEmpty()
                .WithMessage("Tài khoản ngân hàng không được để trống");

            RuleFor(x => x.Amount)
                .GreaterThanOrEqualTo(500_000)
                .WithMessage("Số tiền rút tối thiểu là 500,000 VND")
                .LessThanOrEqualTo(100_000_000) // 100 million VND
                .WithMessage("Số tiền không được vượt quá 100,000,000 VND");
        }
    }
}
