using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Withdraw.Queries;

public sealed class GenerateWithdrawalQRCode
{
    public record Query(Guid WithdrawalRequestId) : IRequest<Result<Response>>;

    public record Response(
        string QrCodeUrl,
        string BankName,
        string AccountName,
        string AccountNumber,
        decimal Amount,
        string Description
    )
    {
        public static async Task<Response> FromEntity(
            WithdrawalRequest withdrawal,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            // Decrypt bank account number
            string decryptedKey = keyManagementService.DecryptKey(
                withdrawal.BankAccount.EncryptionKey.EncryptedKey,
                masterKey
            );

            string decryptedAccountNumber = await aesEncryptionService.Decrypt(
                withdrawal.BankAccount.EncryptedBankAccount,
                decryptedKey,
                withdrawal.BankAccount.EncryptionKey.IV
            );

            // Generate description that includes withdrawal code for tracking
            var description = $"Free driver rut tien";

            // Generate VietQR URL
            var qrUrl =
                $"https://img.vietqr.io/image/{withdrawal.BankAccount.BankInfo.Code}-{decryptedAccountNumber}-compact2.png"
                + $"?amount={withdrawal.Amount}"
                + $"&addInfo={Uri.EscapeDataString(description)}"
                + $"&accountName={Uri.EscapeDataString(withdrawal.BankAccount.BankAccountName)}";

            var bankName =
                $"{withdrawal.BankAccount.BankInfo.ShortName} - {withdrawal.BankAccount.BankInfo.Name}";

            return new(
                qrUrl,
                bankName,
                withdrawal.BankAccount.BankAccountName,
                decryptedAccountNumber,
                withdrawal.Amount,
                description
            );
        }
    }

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Chỉ admin mới có quyền truy cập");

            var withdrawal = await context
                .WithdrawalRequests.Include(w => w.BankAccount)
                .ThenInclude(b => b.BankInfo)
                .Include(w => w.BankAccount)
                .ThenInclude(b => b.EncryptionKey)
                .FirstOrDefaultAsync(w => w.Id == request.WithdrawalRequestId, cancellationToken);

            if (withdrawal == null)
                return Result.NotFound("Không tìm thấy yêu cầu rút tiền");

            if (withdrawal.Status != WithdrawRequestStatusEnum.Pending)
                return Result.Error("Yêu cầu rút tiền này không ở trạng thái chờ xử lý");

            return Result.Success(
                await Response.FromEntity(
                    withdrawal,
                    encryptionSettings.Key,
                    aesEncryptionService,
                    keyManagementService
                ),
                ResponseMessages.Fetched
            );
        }
    }
}
