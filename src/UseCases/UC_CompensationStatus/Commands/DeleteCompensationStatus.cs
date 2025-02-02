
using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_CompensationStatus.Commands;

public class DeleteCompensationStatus
{
    public record Command(Guid Id) : IRequest<Result>;

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Chỉ admin mới có quyền xóa trạng thái bồi thường !");
            CompensationStatus? deletingCompensationStatus = await context.CompensationStatuses
                .Include(cs => cs.Compensations)
                .FirstOrDefaultAsync(cs => cs.Id == request.Id && !cs.IsDeleted, cancellationToken);
            if (deletingCompensationStatus == null)
                return Result.NotFound("Không tìm thấy trạng thái bồi thường !");
            if (deletingCompensationStatus.Compensations.Count != 0)
                return Result.Error("Không thể xóa trạng thái bồi thường đã được sử dụng !");
            deletingCompensationStatus.Delete();
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Xóa trạng thái bồi thường thành công !");
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Thiếu Id !");
        }
    }
}