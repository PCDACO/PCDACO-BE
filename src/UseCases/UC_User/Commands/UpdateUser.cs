using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_User.Commands;

public static class UpdateUser
{
    public record Command(
        Guid Id,
        string Name,
        string Email,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone
    ) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(User user) => new(user.Id);
    }

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Command, Result<Response>>
    {
        private readonly CurrentUser _currentUser = currentUser;
        private readonly IAesEncryptionService _aesEncryptionService = aesEncryptionService;
        private readonly IKeyManagementService _keyManagementService = keyManagementService;
        private readonly EncryptionSettings _encryptionSettings = encryptionSettings;

        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var user = await context.Users.FirstOrDefaultAsync(
                x => x.Id == request.Id && !x.IsDeleted,
                cancellationToken
            );

            if (user is null)
                return Result.NotFound(ResponseMessages.UserNotFound);

            if (_currentUser.User!.Id != user.Id)
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            string decryptedKey = _keyManagementService.DecryptKey(
                user.EncryptionKey.EncryptedKey,
                _encryptionSettings.Key
            );

            string encryptedPhone = await _aesEncryptionService.Encrypt(
                request.Phone,
                decryptedKey,
                user.EncryptionKey.IV
            );

            user.Name = request.Name;
            user.Email = request.Email;
            user.Address = request.Address;
            user.DateOfBirth = request.DateOfBirth;
            user.Phone = encryptedPhone;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(user), ResponseMessages.Updated);
        }
    }
}
