using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_User.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public sealed class GetCurrentUserRoleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/role", Handle)
            .WithSummary("Get current user role")
            .WithTags("Users")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender)
    {
        Result<GetCurrentUserRole.Response> result = await sender.Send(
            new GetCurrentUserRole.Query()
        );
        return result.MapResult();
    }
}
