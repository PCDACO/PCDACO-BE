using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.DTOs;
using UseCases.UC_Car.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class GetCarsForAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars/all", Handle)
            .WithSummary("Get cars for admin")
            .WithTags("Cars")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int pageNumber = 1,
        [FromQuery(Name = "size")] int pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
        )
    {
        Result<OffsetPaginatedResponse<GetCarsForAdmin.Response>> result = await sender.Send(new GetCarsForAdmin.Query(pageNumber, pageSize, keyword!));
        return result.MapResult();
    }
}