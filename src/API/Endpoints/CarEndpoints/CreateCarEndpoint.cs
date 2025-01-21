using API.Utils;

using Ardalis.Result;

using Carter;

using Domain.Enums;

using MediatR;

using UseCases.UC_Car.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class CreateCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cars", Handle)
            .WithSummary("Create a new car")
            .WithTags("Cars")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, CreateCarRequest request)
    {
        Result<CreateCar.Response> result = await sender.Send(new CreateCar.Query(
            request.AmenityIds,
            request.ManufacturerId,
            request.LicensePlate,
            request.Color,
            request.Seat,
            request.Description,
            request.TransmissionType,
            request.FuelType,
            request.FuelConsumption,
            request.RequiresCollateral,
            request.PricePerHour,
            request.PricePerDay,
            request.Latitude,
            request.Longtitude
        ));
        return result.MapResult();
    }
    private sealed record CreateCarRequest(
        Guid[] AmenityIds,
        Guid ManufacturerId,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        TransmissionType TransmissionType,
        FuelType FuelType,
        decimal FuelConsumption,
        bool RequiresCollateral,
        decimal PricePerHour,
        decimal PricePerDay,
        decimal? Latitude,
        decimal? Longtitude
        );
}