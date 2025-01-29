using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.DTOs;
using UseCases.UC_Car.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class GetRentedCarsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        throw new NotImplementedException();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "index")] int pageNumber = 1,
        [FromQuery(Name = "size")] int pageSize = 10,
        [FromQuery(Name = "keyword")] string? keyword = ""
        )
    {
        Result<OffsetPaginatedResponse<GetRentedCars.Response>> result = await sender.Send(new GetRentedCars.Query(pageNumber, pageSize, keyword!));
        return result.MapResult();
    }
}