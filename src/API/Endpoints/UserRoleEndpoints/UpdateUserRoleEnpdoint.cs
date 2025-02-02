using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_UserRole.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserRoleEndpoints;

public class UpdateUserRoleEnpdoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/user-roles/{id:guid}", Handle)
            .WithSummary("Update a user role")
            .WithTags("User Roles")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        UpdateUserRoleRequest request)
    {
        Result result = await sender.Send(new UpdateUserRole.Command(id, request.Name));
        return result.MapResult();
    }
    private record UpdateUserRoleRequest(string Name);
}