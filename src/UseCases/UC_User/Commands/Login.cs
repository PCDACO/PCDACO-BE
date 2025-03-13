using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_User.Commands;

public class Login
{
    public sealed record Command(string Phone, string Password) : IRequest<Result<Response>>;

    public sealed record Response(string AccessToken, string RefreshToken);

    public sealed class Handler(
        IAppDBContext context,
        TokenService tokenService,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var users = await context
                .Users.AsNoTracking()
                .Include(u => u.EncryptionKey)
                .Where(u => !u.IsDeleted)
                .ToListAsync(cancellationToken);

            // Find user by decrypting and comparing phone numbers
            User? user = null;
            foreach (var u in users)
            {
                string decryptedKey = keyManagementService.DecryptKey(
                    u.EncryptionKey.EncryptedKey,
                    encryptionSettings.Key
                );
                string decryptedPhone = await aesEncryptionService.Decrypt(
                    u.Phone,
                    decryptedKey,
                    u.EncryptionKey.IV
                );

                if (decryptedPhone == request.Phone)
                {
                    user = u;
                    break;
                }
            }
            if (user is null)
                return Result.NotFound("Không tìm thấy thông tin người dùng");

            if (user.Password != request.Password.HashString())
                return Result.Error("Sai mật khẩu");

            string newRefreshToken = tokenService.GenerateRefreshToken();
            await context
                .RefreshTokens.Where(rt => rt.UserId == user.Id)
                .ExecuteUpdateAsync(
                    rt => rt.SetProperty(u => u.IsUsed, true).SetProperty(u => u.IsRevoked, true),
                    cancellationToken
                );
            RefreshToken addingRefreshToken = new()
            {
                Token = newRefreshToken,
                UserId = user.Id,
                ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(60),
            };
            await context.RefreshTokens.AddAsync(addingRefreshToken, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            string accessToken = tokenService.GenerateAccessToken(user);
            return Result.Success(
                new Response(accessToken, newRefreshToken),
                "Đăng nhập thành công"
            );
        }
    }
}
