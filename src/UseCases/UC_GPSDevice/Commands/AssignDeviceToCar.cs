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
                .Cars.IgnoreQueryFilters()
                .Include(c => c.InspectionSchedules)
                .Where(c => !c.IsDeleted)
                .Where(c => c.Id == request.CarId)
                .FirstOrDefaultAsync(cancellationToken);
            if (gettingCar is null)
                return Result.Error(ResponseMessages.CarNotFound);

            // Check car must has any inprogress inspection schedule to continue
            if (
                !gettingCar.InspectionSchedules.Any(s =>
                    s.Status == InspectionScheduleStatusEnum.InProgress
                )
            )
                return Result.Error(
                    "Xe chưa có lịch kiểm định nào đang được tiến hành, không thể gán thiết bị GPS !"
                );

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
                gettingDevice.Name = request.DeviceName;
                gettingDevice.Status = DeviceStatusEnum.InUsed;
                gettingDevice.UpdatedAt = DateTime.UtcNow;
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
            }

            // Create location point once, reuse as needed
            var newLocation = geometryFactory.CreatePoint(
                new Coordinate(request.Longtitude, request.Latitude)
            );

            // Find car GPS association
            CarGPS? checkingCarGPS = await context
                .CarGPSes.Where(c => c.CarId == request.CarId)
                .FirstOrDefaultAsync(cancellationToken);

            if (checkingCarGPS is not null)
            {
                // Car already has GPS association
                if (checkingCarGPS.DeviceId == gettingDevice.Id && !checkingCarGPS.IsDeleted)
                {
                    // Error if same device is already actively assigned to car
                    return Result.Error(ResponseMessages.CarGPSIsExisted);
                }

                // Update association
                checkingCarGPS.DeviceId = gettingDevice.Id;
                checkingCarGPS.Location = newLocation;

                // Restore if previously deleted
                if (checkingCarGPS.IsDeleted)
                {
                    checkingCarGPS.Restore();
                }
            }
            else
            {
                // Create new association
                checkingCarGPS = new()
                {
                    DeviceId = gettingDevice.Id,
                    CarId = request.CarId,
                    Location = newLocation,
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
