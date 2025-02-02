
using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_TransmissionType.Commands;

public class UpdateTransmissionType
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
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác");
            // Get transmission type
            TransmissionType? updatingTransmissionType = await context
                .TransmissionTypes
                .Where(x => !x.IsDeleted)
                .Where(x => x.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            // Check if transmission type is exist
            if (updatingTransmissionType is null)
                return Result.NotFound("Không tìm thếu trạng thái");
            // Update database
            updatingTransmissionType.Name = request.Name;
            updatingTransmissionType.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            // Return result
            return Result.SuccessWithMessage("Cập nhật trạng thái thành công");
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Thiếu Id !");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Thiếu tên !");
        }
    }
}