using API.Utils;

using Ardalis.Result;

using Bogus;

using Carter;

using Domain.Enums;

using MediatR;

using UseCases.UC_Car.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class UpdateCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/cars/{id:guid}", Handle)
            .WithSummary("Update a car")
            .WithTags("Cars")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id, UpdateCarRequest request)
    {
        Result result = await sender.Send(new UpdateCar.Commamnd(
            id,
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
    private record UpdateCarRequest(
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