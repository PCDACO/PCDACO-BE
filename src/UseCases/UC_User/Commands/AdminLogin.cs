
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_User.Commands;

public class AdminLogin
{
    public record Command(string Email, string Password) : IRequest<Result<Response>>;
    public record Response(string AccessToken, string RefreshToken);
    public class Handler(
        IAppDBContext context,
        TokenService tokenService
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            User? user = await context
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Email == request.Email && u.Password == request.Password.HashString(),
                    cancellationToken
                );
            if (user is null)
                return Result.NotFound("Không tìm thấy thông tin người dùng");
            string accessToken = tokenService.GenerateAccessToken(user);
            string refreshToken = tokenService.GenerateRefreshToken();
            RefreshToken addingRefreshToken = new()
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiryDate = DateTimeOffset.UtcNow.AddHours(1),
            };
            await context.RefreshTokens.AddAsync(addingRefreshToken, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(
                new Response(accessToken, refreshToken),
                "Đăng nhập thành công"
            );
        }
    }
}