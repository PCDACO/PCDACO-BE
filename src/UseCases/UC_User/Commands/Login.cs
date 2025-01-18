using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_User.Commands;

public class Login
{
    public sealed record Command(
        string Email,
        string Password
    ) : IRequest<Result<Response>>;

    public sealed record Response(
        string AccessToken,
        string RefreshToken
    );

    private sealed class Handler(IAppDBContext context,TokenService tokenService) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            string hashedPassword = request.Password.HashString();
            User? user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == hashedPassword, cancellationToken);
            if (user is null) 
                return Result.NotFound();
            string newRefreshToken = tokenService.GenerateRefreshToken();
            await context.RefreshTokens.Where(rt=>rt.UserId == user.Id)
                .ExecuteUpdateAsync(rt =>rt
                    .SetProperty(u=>u.IsUsed ,true)
                    .SetProperty(u=>u.IsRevoked ,true), cancellationToken);
            RefreshToken addingRefreshToken = new()
            {
                Token = newRefreshToken,
                UserId = user.Id,
                ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(60),
            };
            await context.RefreshTokens.AddAsync(addingRefreshToken, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            string accessToken = tokenService.GenerateAccessToken(user);
            return Result.Success(new Response(accessToken, newRefreshToken));
        }
    }
}