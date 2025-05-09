using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_BankAccount.Commands;

public sealed class CreateBankAccount
{
    public record Command(
        Guid BankInfoId,
        string AccountNumber,
        string AccountName,
        bool IsPrimary = false
    ) : IRequest<Result<Response>>;

    public record Response(Guid Id, string BankName, string AccountName, bool IsPrimary);

    public sealed class Handler(
        IAppDBContext context,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
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

            // Verify bank info exists
            BankInfo? bankInfo = await context
                .BankInfos.AsNoTracking()
                .FirstOrDefaultAsync(
                    b => b.Id == request.BankInfoId && !b.IsDeleted,
                    cancellationToken
                );

            if (bankInfo is null)
                return Result.NotFound(ResponseMessages.BankInfoNotFound);

            // Check for existing account numbers
            // Get all bank accounts of this bank
            var existingAccounts = await context
                .BankAccounts.AsNoTracking()
                .Include(ba => ba.EncryptionKey)
                .Where(ba => ba.BankInfoId == request.BankInfoId)
                .ToListAsync(cancellationToken);

            // Check for duplicate account numbers
            foreach (var account in existingAccounts)
            {
                string decryptedKey = keyManagementService.DecryptKey(
                    account.EncryptionKey.EncryptedKey,
                    encryptionSettings.Key
                );

                string decryptedAccountNumber = await aesEncryptionService.Decrypt(
                    account.EncryptedBankAccount,
                    decryptedKey,
                    account.EncryptionKey.IV
                );

                if (
                    decryptedAccountNumber == request.AccountNumber
                    && account.BankInfoId == request.BankInfoId
                )
                {
                    return Result.Error("Số tài khoản này đã tồn tại trong ngân hàng đã chọn");
                }
            }

            // Check if primary account needs to be updated
            if (request.IsPrimary)
            {
                var existingPrimaryAccounts = await context
                    .BankAccounts.Where(ba => ba.UserId == currentUser.User!.Id && ba.IsPrimary)
                    .ToListAsync(cancellationToken);

                foreach (var account in existingPrimaryAccounts)
                {
                    account.IsPrimary = false;
                }
            }

            // Encrypt the account number
            (string key, string iv) = await keyManagementService.GenerateKeyAsync();
            string encryptedAccountNumber = await aesEncryptionService.Encrypt(
                request.AccountNumber,
                key,
                iv
            );
            string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);

            EncryptionKey encryptionKey = new() { EncryptedKey = encryptedKey, IV = iv };
            await context.EncryptionKeys.AddAsync(encryptionKey, cancellationToken);

            // Create bank account
            BankAccount bankAccount = new()
            {
                UserId = currentUser.User!.Id,
                BankInfoId = request.BankInfoId,
                EncryptionKeyId = encryptionKey.Id,
                EncryptedBankAccount = encryptedAccountNumber,
                BankAccountName = request.AccountName,
                IsPrimary = request.IsPrimary,
            };

            await context.BankAccounts.AddAsync(bankAccount, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new Response(
                    bankAccount.Id,
                    bankInfo.Name,
                    bankAccount.BankAccountName,
                    bankAccount.IsPrimary
                ),
                ResponseMessages.Created
            );
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BankInfoId).NotEmpty().WithMessage("ID ngân hàng không được để trống");

            RuleFor(x => x.AccountNumber)
                .NotEmpty()
                .WithMessage("Số tài khoản không được để trống")
                .MinimumLength(5)
                .WithMessage("Số tài khoản phải có ít nhất 5 ký tự")
                .MaximumLength(20)
                .WithMessage("Số tài khoản không được quá 20 ký tự");

            RuleFor(x => x.AccountName)
                .NotEmpty()
                .WithMessage("Tên tài khoản không được để trống")
                .MinimumLength(3)
                .WithMessage("Tên tài khoản phải có ít nhất 3 ký tự")
                .MaximumLength(100)
                .WithMessage("Tên tài khoản không được quá 100 ký tự");
        }
    }
}
