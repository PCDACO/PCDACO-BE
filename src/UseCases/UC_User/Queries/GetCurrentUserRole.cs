using Ardalis.Result;
using MediatR;
using UseCases.DTOs;

namespace UseCases.UC_User.Queries;

public class GetCurrentUserRole
{
    public sealed record Query() : IRequest<Result<Response>>;

    public sealed record Response(string Role);

    public sealed class Handler(CurrentUser currentUser) : IRequestHandler<Query, Result<Response>>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Result.Success(
                    new Response(currentUser.User!.Role.Name),
                    "Lấy thông tin vai trò người dùng thành công"
                )
            );
        }
    }
}
