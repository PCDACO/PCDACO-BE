using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using UseCases.Abstractions;

namespace UseCases.UC_GPSDevice.Commands;

public class SwitchGPSDeviceForCar
{
    public record Command(Guid CarId, Guid GPSDeviceId, double Longtitude, double Latitude)
        : IRequest<Result<Response>>;

    public record Response(Guid CarId, Guid GPSDeviceId)
    {
        public static Response FromEntity(CarGPS carGPS) => new(carGPS.CarId, carGPS.DeviceId);
    };

    public class Handler(IAppDBContext context, GeometryFactory geometryFactory)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Check if car exists and is not deleted
            Car? car = await context
                .Cars.Where(c => !c.IsDeleted)
                .Where(c => c.Id == request.CarId)
                .FirstOrDefaultAsync(cancellationToken);

            if (car is null)
                return Result.Error(ResponseMessages.CarNotFound);

            // Check if the GPS device exists
            GPSDevice? device = await context
                .GPSDevices.Where(d => !d.IsDeleted)
                .Where(d => d.Id == request.GPSDeviceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (device is null)
                return Result.Error(ResponseMessages.GPSDeviceNotFound);

            // Find car's current GPS association
            CarGPS? currentCarGPS = await context
                .CarGPSes.Where(c => c.DeviceId == request.GPSDeviceId && !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            // Create location point once, reuse as needed
            var newLocation = geometryFactory.CreatePoint(
                new Coordinate(request.Longtitude, request.Latitude)
            );

            if (currentCarGPS != null)
            {
                // set the old car status to be pending
                await context
                    .Cars.IgnoreQueryFilters()
                    .Where(c => c.Id == currentCarGPS.CarId)
                    .ExecuteUpdateAsync(
                        c => c.SetProperty(c => c.Status, CarStatusEnum.Pending),
                        cancellationToken
                    );
                // switch the GPS device for new the car
                currentCarGPS.CarId = request.CarId;
                currentCarGPS.Location = newLocation;
                currentCarGPS.IsDeleted = false;
            }
            else
            {
                // Create a new association for the car and GPS device
                currentCarGPS = new CarGPS()
                {
                    CarId = request.CarId,
                    DeviceId = request.GPSDeviceId,
                    Location = newLocation,
                };
                await context.CarGPSes.AddAsync(currentCarGPS, cancellationToken);
            }
            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage("Cập nhật thiết bị GPS cho xe thành công");
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CarId).NotEmpty().WithMessage("ID xe không được để trống");
            RuleFor(x => x.GPSDeviceId)
                .NotEmpty()
                .WithMessage("ID thiết bị GPS không được để trống");
        }
    }
}
