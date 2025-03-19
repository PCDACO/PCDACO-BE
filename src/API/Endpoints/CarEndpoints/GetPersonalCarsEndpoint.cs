using API.Utils;

using Ardalis.Result;

using Carter;

using Domain.Enums;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using UseCases.DTOs;
using UseCases.UC_Car.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class GetPersonalCarsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cars/personal", Handle)
            .WithSummary("Get personal cars")
            .WithTags("Cars")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "model")] Guid? model,
        [FromQuery(Name = "lastId")] Guid? lastCarId,
        [FromQuery(Name = "amenities")] Guid[]? amenities,
        [FromQuery(Name = "fuel")] Guid? fuel,
        [FromQuery(Name = "transmission")] Guid? transmission,
        [FromQuery(Name = "limit")] int? limit = 10,
        [FromQuery(Name = "status")] CarStatusEnum status = CarStatusEnum.Available
    )
    {
        Result<OffsetPaginatedResponse<GetPersonalCars.Response>> result = await sender.Send(new GetPersonalCars.Query(
            model,
            amenities,
            fuel,
            transmission,
            lastCarId,
            limit!.Value,
            status
        ));
        return result.MapResult();
    }
}