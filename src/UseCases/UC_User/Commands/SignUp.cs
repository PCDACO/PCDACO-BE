
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_User.Commands;

public class SignUp
{
    public record Command(
        string Name,
        string Email,
        string Password,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone
    ) : IRequest<Result<Response>>;

    public record Response(
        string AccessToken,
        string RefreshToken
    );

    private sealed class Handler(IAppDBContext context, TokenService tokenService) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            string refreshToken = tokenService.GenerateRefreshToken();
            EncryptionKey encryptionKey = new()
            {
                EncryptedKey = StringGenerator.GenerateRandomString(),
            };
            await context.EncryptionKeys.AddAsync(encryptionKey, cancellationToken);
            User user = new()
            {
                EncryptionKeyId = encryptionKey.Id,
                Name = request.Name,
                Email = request.Email,
                Password = request.Password.HashString(),
                Address = request.Address,
                DateOfBirth = request.DateOfBirth,
                Phone = request.Phone,
            };
            user.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiryDate = DateTimeOffset.UtcNow,
            });
            await context.Users.AddAsync(user, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            string accessToken = tokenService.GenerateAccessToken(user);
            return Result.Created(new Response(accessToken, refreshToken));
        }
    }
}