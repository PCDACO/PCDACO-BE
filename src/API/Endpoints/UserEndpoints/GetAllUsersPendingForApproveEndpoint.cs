using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.DTOs;
using UseCases.UC_User.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class GetAllUsersPendingForApproveEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/license/approve", Handle)
            .WithSummary("Get all users pending license approval filter by name or email of user")
            .WithTags("Users")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetAllUsersPendingForApprove.Response>> result =
            await sender.Send(
                new GetAllUsersPendingForApprove.Query(pageNumber!.Value, pageSize!.Value, keyword!)
            );
        return result.MapResult();
    }
}
