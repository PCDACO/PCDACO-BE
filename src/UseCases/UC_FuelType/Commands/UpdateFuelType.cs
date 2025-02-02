
using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_FuelType.Commands;

public class UpdateFuelType
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
            // Check permission
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền truy cập");
            // Get transmission type
            FuelType? updatingFuelType = await context.FuelTypes
                .Where(x => !x.IsDeleted)
                .Where(x => x.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (updatingFuelType is null)
                return Result.NotFound("Không tìm thấy loại nhiên liệu");
            // Update transmission type
            updatingFuelType.Name = request.Name;
            updatingFuelType.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            // Return result
            return Result.SuccessWithMessage("Cập nhật loại nhiên liệu thành cong");

        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Thiếu Id !");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Thiếu tên loại nhiên liệu !");
        }
    }
}