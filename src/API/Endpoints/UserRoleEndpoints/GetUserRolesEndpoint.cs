using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.DTOs;
using UseCases.UC_UserRole.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserRoleEndpoints;

public class GetUserRolesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/user-roles", Handle)
            .WithSummary("Get user roles")
            .WithTags("User Roles")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
        )
    {
        Result<OffsetPaginatedResponse<GetUserRoles.Response>> result =
            await sender.Send(new GetUserRoles.Query(pageNumber!.Value, pageSize!.Value, keyword!));
        return result.MapResult();
    }

}