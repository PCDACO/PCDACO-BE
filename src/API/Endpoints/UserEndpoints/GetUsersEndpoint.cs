using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_User.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class GetUsersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users", Handle);
    }
    private async Task<IResult> Handle(ISender sender)
    {
        Result<GetUsers.Response> result = await sender.Send(new GetUsers.Query());
        return result.MapResult();
    }
}