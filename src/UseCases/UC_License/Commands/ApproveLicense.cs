using Ardalis.Result;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_License.Commands;

public sealed class ApproveLicense
{
    public sealed record Command(Guid LicenseId, bool IsApproved, string? RejectReason = null)
        : IRequest<Result<Response>>;

    public sealed record Response(Guid LicenseId)
    {
        public static Response FromEntity(License license)
        {
            return new(license.Id);
        }
    };

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        private readonly IAppDBContext _context = context;
        private readonly CurrentUser _currentUser = currentUser;

        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!_currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");

            var license = await _context.Licenses.FirstOrDefaultAsync(
                l => l.Id == request.LicenseId && !l.IsDeleted,
                cancellationToken
            );

            if (license == null)
                return Result.NotFound("Không tìm thấy giấy phép lái xe");

            if (license.IsApprove != null)
                return Result.Conflict("Giấy phép này đã được xử lý");

            // If rejecting, require a reason
            if (!request.IsApproved && string.IsNullOrWhiteSpace(request.RejectReason))
                return Result.Error("Phải cung cấp lý do từ chối giấy phép");

            license.IsApprove = request.IsApproved;
            license.ApprovedAt = !request.IsApproved ? null : DateTimeOffset.UtcNow;
            license.RejectReason = !request.IsApproved ? request.RejectReason : null;

            await _context.SaveChangesAsync(cancellationToken);

            string message = request.IsApproved ? "phê duyệt" : "từ chối";
            return Result.Success(
                Response.FromEntity(license),
                $"Đã {message} giấy phép lái xe thành công"
            );
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.LicenseId).NotEmpty().WithMessage("ID giấy phép không được để trống");

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
