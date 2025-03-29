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

public class AssignDeviceToCar
{
    public record Command(
        Guid CarId,
        string OSBuildId,
        string DeviceName,
        double Longtitude,
        double Latitude
    ) : IRequest<Result>;

    public class Handler(IAppDBContext context, GeometryFactory geometryFactory)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // check car
            Car? gettingCar = await context
                .Cars.Where(c => !c.IsDeleted)
                .Where(c => c.Id == request.CarId)
                .FirstOrDefaultAsync(cancellationToken);
            if (gettingCar is null)
                return Result.Error(ResponseMessages.CarNotFound);
            // check device
            GPSDevice? gettingDevice = await context
                .GPSDevices.Where(d => d.OSBuildId == request.OSBuildId)
                .FirstOrDefaultAsync(cancellationToken);

            // add new device
            if (gettingDevice is not null)
            {
                if (gettingDevice.Status != DeviceStatusEnum.Available)
                {
                    return Result.Error(ResponseMessages.GPSDeviceIsNotAvailable);
                }
                gettingDevice.Status = DeviceStatusEnum.InUsed;
            }
            else
            {
                gettingDevice = new GPSDevice()
                {
                    OSBuildId = request.OSBuildId,
                    Name = request.DeviceName,
                    Status = DeviceStatusEnum.InUsed,
                };
                await context.GPSDevices.AddAsync(gettingDevice, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            /// assign device to car
            CarGPS? checkingCarGPS = await context
                .CarGPSes.Where(c => c.CarId == request.CarId)
                .Where(c => c.DeviceId == gettingDevice.Id)
                .Where(c => !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
            if (checkingCarGPS is not null)
            {
                if (!checkingCarGPS.IsDeleted)
                    return Result.Error(ResponseMessages.CarGPSIsExisted);
                checkingCarGPS.Restore();
                checkingCarGPS.Location = geometryFactory.CreatePoint(
                    new Coordinate(request.Longtitude, request.Latitude)
                );
            }
            else
            {
                checkingCarGPS = new()
                {
                    DeviceId = gettingDevice.Id,
                    CarId = request.CarId,
                    Location = geometryFactory.CreatePoint(
                        new Coordinate(request.Longtitude, request.Latitude)
                    ),
                };
                await context.CarGPSes.AddAsync(checkingCarGPS, cancellationToken);
            }
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage(ResponseMessages.Created);
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CarId).NotEmpty().WithMessage("Phải chọn 1 xe !");
            RuleFor(x => x.OSBuildId)
                .NotEmpty()
                .WithMessage("ID bản dựng hệ điều hành của thiết bị không được để trống !");
            RuleFor(x => x.DeviceName).NotEmpty().WithMessage("Tên thiết bị không được để trống !");
        }
    }
}
