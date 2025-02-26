
using Ardalis.Result;

using Domain.Constants;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using NetTopologySuite.Geometries;

using UseCases.Abstractions;

namespace UseCases.UC_GPSDevice.Commands;

public class AssignDeviceToCar
{
    public record Command(
     Guid CarId,
     Guid DeviceId,
     double Longtitude,
     double Latitude
    ) : IRequest<Result>;

    public class Handler(
        IAppDBContext context,
        GeometryFactory geometryFactory
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // check car
            Car? gettingCar = await context.Cars
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Where(c => c.Id == request.CarId)
                .FirstOrDefaultAsync(cancellationToken);
            if (gettingCar is null) return Result.Error(ResponseMessages.CarNotFound);
            // check device
            GPSDevice? gettingDevice = await context.GPSDevices
                .AsNoTracking()
                .Where(d => !d.IsDeleted)
                .Where(d => d.Id == request.DeviceId)
                .FirstOrDefaultAsync(cancellationToken);
            if (gettingDevice is null) return Result.Error(ResponseMessages.GPSDeviceNotFound);
            // check car gps checking status
            CarGPS? checkingCarGPS = await context.CarGPSes
                .Where(c => c.CarId == request.CarId)
                .Where(c => c.DeviceId == request.DeviceId)
                .FirstOrDefaultAsync(cancellationToken);
            if (checkingCarGPS is not null)
            {
                if (!checkingCarGPS.IsDeleted) return Result.Error(ResponseMessages.CarGPSIsExisted);
                checkingCarGPS.Restore();
                checkingCarGPS.Location = geometryFactory.CreatePoint(
                        new Coordinate(request.Latitude, request.Longtitude)
                );
            }
            else
            {
                checkingCarGPS = new()
                {
                    DeviceId = request.DeviceId,
                    CarId = request.CarId,
                    Location = geometryFactory.CreatePoint(
                        new Coordinate(request.Latitude, request.Longtitude)
                    )
                };
                context.CarGPSes.Add(checkingCarGPS);
            }
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage(ResponseMessages.Created);
        }
    }

}