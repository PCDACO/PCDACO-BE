using API.Utils;
using Ardalis.Result;
using Bogus;
using Carter;
using Domain.Entities;
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
        Result result = await sender.Send(
            new UpdateCar.Commamnd(
                CarId: id,
                AmenityIds: request.AmenityIds,
                ModelId: request.ModelId,
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
            )
        );
        return result.MapResult();
    }

    private record UpdateCarRequest(
        Guid[] AmenityIds,
        Guid ModelId,
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
