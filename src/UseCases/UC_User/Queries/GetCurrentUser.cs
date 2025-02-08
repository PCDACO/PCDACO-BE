using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_User.Queries;

public static class GetCurrentUser
{
    public record Query() : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string Name,
        string Email,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone,
        string Role
    )
    {
        public static async Task<Response> FromEntityAsync(
            User user,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            string decryptedKey = keyManagementService.DecryptKey(
                user.EncryptionKey.EncryptedKey,
                masterKey
            );
            string decryptedPhoneNumber = await aesEncryptionService.Decrypt(
                user.Phone,
                decryptedKey,
                user.EncryptionKey.IV
            );
            return new(
                user.Id,
                user.Name,
                user.Email,
                user.Address,
                user.DateOfBirth,
                decryptedPhoneNumber,
                user.Role.Name
            );
        }
    };

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
            if (currentUser.User is null)
                return Result.Unauthorized("Bạn chưa đăng nhập");
            User? user = await context
                .Users.AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.EncryptionKey)
                .FirstOrDefaultAsync(
                    u => u.Id == currentUser.User!.Id && !u.IsDeleted,
                    cancellationToken
                );

            if (user is null)
                return Result.NotFound("Không tìm thấy thông tin người dùng");

            return Result.Success(
                await Response.FromEntityAsync(
                    user: user!,
                    masterKey: encryptionSettings.Key,
                    aesEncryptionService: aesEncryptionService,
                    keyManagementService: keyManagementService
                ),
                "Lấy thông tin người dùng thành công"
            );
        }
    }
}
