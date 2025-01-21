using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_User.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class CreateAdminUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/admin", Handle)
            .WithSummary("Create an admin user")
            .WithTags("Users");
    }

    private async Task<IResult> Handle(ISender sender)
    {
        Result result = await sender.Send(new CreateAdminUser.Command());
        return result.MapResult();
    }
}