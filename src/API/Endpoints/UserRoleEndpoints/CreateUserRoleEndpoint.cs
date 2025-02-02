using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.UC_UserRole.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserRoleEndpoints;

public class CreateUserRoleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/user-roles", Handle)
            .WithSummary("Create a new user role")
            .WithTags("User Roles")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        CreateUserRoleRequest request
    )
    {
        Result<CreateUserRole.Response> result = await sender.Send(new CreateUserRole.Command(request.Name));
        return result.MapResult();
    }
    private record CreateUserRoleRequest(string Name);
}