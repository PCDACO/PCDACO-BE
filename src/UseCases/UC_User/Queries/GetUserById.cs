using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_User.Queries;

public class GetUserById
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

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
        public static async Task<Response> FromEntity(
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
            string decryptedPhone = await aesEncryptionService.Decrypt(
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
                decryptedPhone,
                user.Role.Name
            );
        }
    }

    internal sealed class Handler(
        IAppDBContext context,
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
            var user = await context
                .Users.AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.EncryptionKey)
                .FirstOrDefaultAsync(u => u.Id == request.Id && !u.IsDeleted, cancellationToken);

            if (user is null)
                return Result.NotFound(ResponseMessages.UserNotFound);

            var response = await Response.FromEntity(
                user,
                encryptionSettings.Key,
                aesEncryptionService,
                keyManagementService
            );

            return Result.Success(response, ResponseMessages.Fetched);
        }
    }
}
