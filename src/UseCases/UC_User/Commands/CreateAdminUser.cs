using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_User.Commands;

public sealed class CreateAdminUser
{
    public record Command() : IRequest<Result>;

    private class Handler(
        IAppDBContext context,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            string adminEmail = "admin@gmail.com";
            User? checkingAdmin = await context.Users.FirstOrDefaultAsync(
                u => u.Email == adminEmail,
                cancellationToken
            );
            if (checkingAdmin is not null)
                return Result.Forbidden("Tài khoản đã được khởi tạo !");
            //
            string adminName = "Admin";
            string adminPhoneNumber = "0123456789";
            string adminPassword = "admin";
            string adminAddress = "Hanoi";
            var (key, iv) = await keyManagementService.GenerateKeyAsync();
            string encryptedKey = keyManagementService.EncryptKey(key, encryptionSettings.Key);
            string encryptedPhoneNumber = await aesEncryptionService.Encrypt(
                adminPhoneNumber,
                key,
                iv
            );
            EncryptionKey newEncryptionKey = new() { EncryptedKey = encryptedKey, IV = iv };
            await context.EncryptionKeys.AddAsync(newEncryptionKey, cancellationToken);
            User admin = new()
            {
                Name = adminName,
                Password = adminPassword.HashString(),
                Email = adminEmail,
                Address = adminAddress,
                DateOfBirth = DateTimeOffset.UtcNow,
                Phone = encryptedPhoneNumber,
                Role = UserRole.Admin,
                EncryptionKeyId = newEncryptionKey.Id,
            };
            await context.Users.AddAsync(admin, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Tạo tài khoản admin thành công");
        }
    }
}
