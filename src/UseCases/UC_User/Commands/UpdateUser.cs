using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using FluentValidation;
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
        private readonly IAppDBContext _context = context;
        private readonly CurrentUser _currentUser = currentUser;
        private readonly IAesEncryptionService _aesEncryptionService = aesEncryptionService;
        private readonly IKeyManagementService _keyManagementService = keyManagementService;
        private readonly EncryptionSettings _encryptionSettings = encryptionSettings;

        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var user = await _context.Users.FirstOrDefaultAsync(
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
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(user), ResponseMessages.Updated);
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Tên không được để trống")
                .MinimumLength(5)
                .WithMessage("Tên phải có ít nhất 3 ký tự")
                .MaximumLength(50)
                .WithMessage("Tên không được quá 50 ký tự");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email không được để trống")
                .EmailAddress()
                .WithMessage("Email không hợp lệ");

            RuleFor(x => x.Address).NotEmpty().WithMessage("Địa chỉ không được để trống");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty()
                .WithMessage("Ngày sinh không được để trống")
                .LessThan(DateTimeOffset.UtcNow)
                .WithMessage("Ngày sinh phải nhỏ hơn ngày hiện tại");

            RuleFor(x => x.Phone).NotEmpty().WithMessage("Số điện thoại không được để trống");
        }
    }
}
