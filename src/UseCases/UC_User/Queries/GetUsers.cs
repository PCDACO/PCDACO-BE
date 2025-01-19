using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.UC_User.Queries;

public class GetUsers
{
    public sealed record Query : IRequest<Result<Response>>;

    public sealed record Response(
        IEnumerable<User> Users
    );

    public class Handler(IAppDBContext context) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            IEnumerable<User> users = await context.Users.ToListAsync(cancellationToken);
            return Result.Success(new Response(users));
        }
    }
}