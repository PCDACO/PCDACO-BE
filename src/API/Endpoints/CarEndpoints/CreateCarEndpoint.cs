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
            AmenityIds: request.AmenityIds,
            ManufacturerId: request.ManufacturerId,
            TransmissionTypeId: request.TransmissionTypeId,
            FuelTypeId: request.FuelTypeId,
            LicensePlate: request.LicensePlate,
            Color: request.Color,
            Seat: request.Seat,
            Description: request.Description,
            FuelConsumption: request.FuelConsumption,
            RequiresCollateral: request.RequiresCollateral,
            PricePerHour: request.PricePerHour,
            PricePerDay: request.PricePerDay,
            Latitude: request.Latitude,
            Longtitude: request.Longtitude
        ));
        return result.MapResult();
    }
    private sealed record CreateCarRequest(
        Guid[] AmenityIds,
        Guid ManufacturerId,
        Guid TransmissionTypeId,
        Guid FuelTypeId,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        decimal FuelConsumption,
        bool RequiresCollateral,
        decimal PricePerHour,
        decimal PricePerDay,
        decimal? Latitude,
        decimal? Longtitude
        );
}