using Ardalis.Result;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_Auth.Commands;

public static class RefreshUserToken
{
    public record Command(string RefreshToken) : IRequest<Result<TokenResponse>>;

    public record TokenResponse(string AccessToken, string RefreshToken);

    public class Handler(IAppDBContext context, TokenService tokenService)
        : IRequestHandler<Command, Result<TokenResponse>>
    {
        public async Task<Result<TokenResponse>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var refreshToken = await context
                .RefreshTokens.Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            if (refreshToken == null)
            {
                return Result.Error("Token làm mới không hợp lệ");
            }

            if (refreshToken.RevokedAt != null)
            {
                return Result.Error("Token đã bị thu hồi");
            }

            var user = refreshToken.User;
            var newAccessToken = tokenService.GenerateAccessToken(user);
            var newRefreshToken = tokenService.GenerateRefreshToken();

            // Revoke the old refresh token
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;

            // Create new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = user.Id,
                ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(60),
            };

            await context.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new TokenResponse(newAccessToken, newRefreshToken),
                "Làm mới token thành công"
            );
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .WithMessage("Token làm mới không được để trống");
        }
    }
}
