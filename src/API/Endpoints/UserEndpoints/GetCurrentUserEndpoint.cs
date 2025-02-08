using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_User.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class GetCurrentUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/current", Handle)
            .WithSummary("Get current user information")
            .WithTags("Users")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender)
    {
        Result<GetCurrentUser.Response> result = await sender.Send(new GetCurrentUser.Query());
        return result.MapResult();
    }
}
