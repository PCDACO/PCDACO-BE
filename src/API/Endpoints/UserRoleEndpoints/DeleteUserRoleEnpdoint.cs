using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_UserRole.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserRoleEndpoints;

public class DeleteUserRoleEnpdoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/user-roles/{id:guid}", Handle)
            .WithSummary("Delete a user role")
            .WithTags("User Roles")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
         Guid id)
    {
        Result result = await sender.Send(new DeleteUserRole.Command(id));
        return result.MapResult();
    }
}