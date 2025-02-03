using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_UserRole.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserRoleEndpoints;

public class GetUserRoleByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/user-roles/{id:guid}", Handle)
            .WithSummary("Get user role by id")
            .WithTags("User Roles")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        Guid id
    )
    {
        Result<GetUserRoleById.Response> result = await sender.Send(new GetUserRoleById.Query(id));
        return result.MapResult();
    }
}