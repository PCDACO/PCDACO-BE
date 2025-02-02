
using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_TransmissionType.Commands;

public class CreateTransmissionType
{
    public record Command(
        string Name
    ) : IRequest<Result<Response>>;

    public record Response(
        Guid Id
    )
    {
        public static Response FromEntity(TransmissionType entity) => new(entity.Id);
    };

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác");

            TransmissionType addingTransmissionType = new()
            {
                Name = request.Name
            };
            await context.TransmissionTypes.AddAsync(addingTransmissionType, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(Response.FromEntity(addingTransmissionType), "Tạo thành công");
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Thiếu tên !");
        }
    }
}