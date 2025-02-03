using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_ContractStatus.Commands;

public class CreateContractStatus
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
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác này");
            // Create
            ContractStatus addingContractionStatus = new()
            {
                Name = request.Name
            };
            // Add to db
            await context.ContractStatuses.AddAsync(addingContractionStatus, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            // Return result
            return Result.Success(new Response(addingContractionStatus.Id), "Tạo trạng thái hợp đồng thành công");
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Thiếu tên trạng thái !");
        }
    }
}
