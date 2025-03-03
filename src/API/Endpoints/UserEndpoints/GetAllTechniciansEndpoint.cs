using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.DTOs;
using UseCases.UC_User.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public sealed class GetAllTechniciansEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users/technicians", Handle)
            .WithSummary("Get all technicians")
            .WithTags("Users")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int? pageNumber = 1,
        [FromQuery(Name = "size")] int? pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
    )
    {
        Result<OffsetPaginatedResponse<GetAllTechnicians.Response>> result = await sender.Send(
            new GetAllTechnicians.Query(
                PageNumber: pageNumber!.Value,
                PageSize: pageSize!.Value,
                Keyword: keyword!
            )
        );
        return result.MapResult();
    }
}
