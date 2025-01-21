using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;

namespace UseCases.UC_Car.Queries;

public class GetCarById
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid Id,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        string TransmissionType,
        string FuelType,
        decimal FuelConsumption,
        bool RequiresCollateral,
        decimal PricePerHour,
        decimal PricePerDay,
        double? Longtitude,
        double? Latitude,
        string ManufacturerName
    )
    {
        public static Response FromEntity(Car car)
            => new(
                car.Id,
                car.EncryptedLicensePlate,
                car.Color,
                car.Seat,
                car.Description,
                car.TransmissionType.ToString(),
                car.FuelType.ToString(),
                car.FuelConsumption,
                car.RequiresCollateral,
                car.PricePerHour,
                car.PricePerDay,
                car.Location.X,
                car.Location.Y,
                car.Manufacturer.Name
            );
    };

    private sealed class Handler(IAppDBContext context) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
            => await context.Cars
                .Include(c => c.Manufacturer)
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken)
                switch
            {
                null => Result<Response>.NotFound(),
                var car => Result<Response>.Success(Response.FromEntity(car))
            };
    }
}