
using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_CarStatus.Commands;

public class UpdateCarStatus
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
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác này");

            CarStatus? updatingCarStatus = await context.CarStatuses
                .Where(x => !x.IsDeleted)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (updatingCarStatus is null)
                return Result.NotFound("Không tìm thấy trạng thái xe");
            
            updatingCarStatus.Name = request.Name;
            updatingCarStatus.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Cập nhật trạng thái xe thành công");
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Thiếu Id !");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Thiếu tên trạng thái !");
        }
    }
}