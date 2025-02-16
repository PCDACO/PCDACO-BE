using Ardalis.Result;

using Domain.Entities;
using Domain.Shared;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_User.Commands;

public sealed class CreateAdminUser
{
    public record Command() : IRequest<Result>;

    public class Handler(
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
            UserRole? checkingAdminRole = await context.UserRoles.FirstOrDefaultAsync(
                ur => EF.Functions.ILike(ur.Name, "%admin%"),
                cancellationToken
            );
            if (checkingAdminRole is null)
                return Result.Error("Không thể tạo tài khoản admin");
            // Add admin Database
            bool isSuccess = Guid.TryParse("01950d41-d234-7b63-a360-72b27605b4a4", out Guid adminId);
            if (!isSuccess) throw new Exception("can not parse admin id");
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
            User admin =
                new()
                {
                    Id = adminId,
                    Name = adminName,
                    Password = adminPassword.HashString(),
                    Email = adminEmail,
                    Address = adminAddress,
                    DateOfBirth = DateTimeOffset.UtcNow,
                    Phone = encryptedPhoneNumber,
                    RoleId = checkingAdminRole.Id,
                    EncryptionKeyId = newEncryptionKey.Id,
                };
            await context.Users.AddAsync(admin, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Tạo tài khoản admin thành công");
        }
    }
}
