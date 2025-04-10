using Ardalis.Result;
using Domain.Entities;
using Domain.Shared.EmailTemplates.EmailBookings;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Services.EmailService;

namespace UseCases.UC_License.Commands;

public sealed class ApproveLicense
{
    public sealed record Command(Guid UserId, bool IsApproved, string? RejectReason = null)
        : IRequest<Result<Response>>;

    public sealed record Response(Guid UserId, bool IsApproved, string? RejectReason)
    {
        public static Response FromEntity(User user)
        {
            return new(user.Id, user.LicenseIsApproved!.Value, user.LicenseRejectReason);
        }
    };

    internal sealed class Handler(
        IAppDBContext context,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        private readonly IAppDBContext _context = context;
        private readonly IEmailService _emailService = emailService;
        private readonly CurrentUser _currentUser = currentUser;

        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!_currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");

            var user = await _context.Users.FirstOrDefaultAsync(
                u => u.Id == request.UserId && !u.IsDeleted,
                cancellationToken
            );

            if (user is null)
                return Result.Error("Người dùng không tồn tại");

            if (string.IsNullOrEmpty(user.EncryptedLicenseNumber))
                return Result.NotFound("Không tìm thấy giấy phép lái xe");

            if (user.LicenseIsApproved != null)
                return Result.Conflict("Giấy phép này đã được xử lý");

            // If rejecting, require a reason
            if (!request.IsApproved && string.IsNullOrWhiteSpace(request.RejectReason))
                return Result.Error("Phải cung cấp lý do từ chối giấy phép");

            user.LicenseIsApproved = request.IsApproved;
            user.LicenseApprovedAt = !request.IsApproved ? null : DateTimeOffset.UtcNow;
            user.LicenseRejectReason = !request.IsApproved ? request.RejectReason : null;

            await _context.SaveChangesAsync(cancellationToken);

            // Send email notification
            var emailContent = LicenseApprovalTemplate.Template(
                user.Name,
                request.IsApproved,
                request.RejectReason
            );

            backgroundJobClient.Enqueue(
                () =>
                    _emailService.SendEmailAsync(
                        user.Email,
                        "Thông Báo Phê Duyệt Giấy Phép Lái Xe",
                        emailContent
                    )
            );

            string message = request.IsApproved ? "phê duyệt" : "từ chối";
            return Result.Success(
                Response.FromEntity(user),
                $"Đã {message} giấy phép lái xe thành công"
            );
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IsApproved)
                .NotNull()
                .WithMessage("Trạng thái phê duyệt không được để trống");

            When(
                x => !x.IsApproved,
                () =>
                    RuleFor(x => x.RejectReason)
                        .NotEmpty()
                        .WithMessage("Phải cung cấp lý do từ chối")
                        .MaximumLength(500)
                        .WithMessage("Lý do từ chối không được vượt quá 500 ký tự")
            );
        }
    }
}
