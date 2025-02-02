
using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_UserRole.Commands;

public class CreateUserRole
{
    public record Command(
        string Name
    ) : IRequest<Result<Response>>;

    public record Response(Guid Id);

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check if user is admin
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác");
            // Create user role
            UserRole addingUserRole = new()
            {
                Name = request.Name
            };
            // Add user role
            await context.UserRoles.AddAsync(addingUserRole, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            // Return result
            return Result.Success(new Response(addingUserRole.Id), "Tạo vai trò người dùng thành công");

        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Thiếu tên vai trò");
        }
    }
}