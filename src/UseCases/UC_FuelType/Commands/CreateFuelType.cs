
using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_FuelType.Commands;

public class CreateFuelType
{
    public record Command(
        string Name
    ) : IRequest<Result<Response>>;

    public record Response(
        Guid Id
    );

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check permission
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Chỉ admin mới có quyền thực hiện chức năng này");
            // Create transmission type object
            FuelType addingFuelType = new()
            {
                Name = request.Name
            };
            // Add transmission type
            await context.FuelTypes.AddAsync(addingFuelType, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            // Return result
            return Result.Success(new Response(addingFuelType.Id), "Tạo loại nhiên liệu thành công");
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Thiếu tên loại nhiên liệu !");
        }
    }
}