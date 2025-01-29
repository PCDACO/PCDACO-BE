using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_CompensationStatus.Commands;

public class CreateCompensationStatus
{
    public record Command(string Name) : IRequest<Result<Response>>;

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
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác này");
            CompensationStatus addingCompensationStatus = new()
            {
                Name = request.Name
            };
            await context.CompensationStatuses.AddAsync(addingCompensationStatus, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(new Response(addingCompensationStatus.Id), "Thêm trạng thái đặt bồi thường thành công");
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên trạng thái bồi thường không được để trống");
        }
    }
}