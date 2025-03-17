using API.Utils;

using Ardalis.Result;

using Carter;

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
        Result<UpdateCar.Response> result = await sender.Send(
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
                Price: request.Price,
                FuelConsumption: request.FuelConsumption,
                RequiresCollateral: request.RequiresCollateral,
                PickupLatitude: request.PickupLatitude,
                PickupLongitude : request.PickupLongitude,
                PickupAddress: request.PickupAddress
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
        decimal Price,
        decimal PickupLatitude,
        decimal PickupLongitude,
        string PickupAddress
    );
}
