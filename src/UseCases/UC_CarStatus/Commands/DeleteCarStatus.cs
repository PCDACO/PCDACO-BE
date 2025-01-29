
using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_CarStatus.Commands;

public class DeleteCarStatus
{
    public record Command(
        Guid Id
    ) : IRequest<Result>;

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác này");

            CarStatus? deletingCarStatus = await context.CarStatuses
                .Include(x => x.Cars)
                .Where(x => !x.IsDeleted)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (deletingCarStatus is null)
                return Result.NotFound("Không tìm thấy trạng thái xe");

            if (deletingCarStatus.Cars.Count != 0)
                return Result.Error("Không thể xóa trạng thái xe đã được sử dụng");

            deletingCarStatus.Delete();
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Xóa trạng thái xe thành công");
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