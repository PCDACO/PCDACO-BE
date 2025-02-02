
using Ardalis.Result;

using Domain.Entities;

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
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác");
            UserRole addingUserRole = new()
            {
                Name = request.Name
            };
            await context.UserRoles.AddAsync(addingUserRole, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(new Response(addingUserRole.Id), "Tạo vai trò người dùng thành công");

        }
    }
}