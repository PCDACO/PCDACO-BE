using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.DTOs;
using UseCases.UC_Car.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public sealed class GetAmenitiesOfCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars/{id:guid}/amenities", Handle)
            .WithSummary("Get amenities of a car")
            .WithTags("Cars")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id,
        [AsParameters] GetAmenitiesOfCarRequest request)
    {
        Result<OffsetPaginatedResponse<GetAmenitiesOfCar.Response>> result = await sender.Send(
            new GetAmenitiesOfCar.Query(id,
                                        request.pageNumber!.Value,
                                        request.pageSize!.Value,
                                        request.keyword!));
        return result.MapResult();
    }

    private record GetAmenitiesOfCarRequest(
        string? keyword,
        int? pageNumber = 1,
        int? pageSize = 10
    );
}