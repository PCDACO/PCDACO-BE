using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.DTOs;
using UseCases.UC_Manufacturer.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ManufacturerEndpoints;

public class GetAllManufacturersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/manufacturers", Handle)
            .WithSummary("Get all manufacturers")
            .WithTags("Manufacturers")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [AsParameters] GetAllManufacturersRequest request
    )
    {
        Result<OffsetPaginatedResponse<GetAllManufacturers.Response>> result = await sender.Send(
            new GetAllManufacturers.Query(
                request.pageNumber!.Value,
                request.pageSize!.Value,
                request.keyword!
            )
        );
        return result.MapResult();
    }

    private record GetAllManufacturersRequest(
        int? pageNumber = 1,
        int? pageSize = 10,
        string? keyword = ""
    );
}
