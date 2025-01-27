
using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_CompensationStatus.Commands;

public class UpdateCompensationStatus
{
    public record Command(
        Guid Id,
        string Name
    ) : IRequest<Result>;

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Unauthorized("Chỉ admin mới có quyền thực hiện chức năng này");
            CompensationStatus? updatingCompensationStatus = await context.CompensationStatuses
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (updatingCompensationStatus == null)
                return Result.NotFound("Không tìm thấy trạng thái bồi thường");
            // Update database
            updatingCompensationStatus.Name = request.Name;
            updatingCompensationStatus.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Cập nhật trạng thái bồi thường thành công");
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Thiếu Id");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Thiếu tên trạng thái bồi thường");
        }
    }
}