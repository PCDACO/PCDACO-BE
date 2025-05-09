using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_GPSDevice.Commands;

public class AssignDeviceToCar
{
    public record Command(
        Guid CarId,
        string OSBuildId,
        string DeviceName,
        double Longtitude,
        double Latitude
    ) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(GPSDevice gpsDevice) => new(gpsDevice.Id);
    };

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        GeometryFactory geometryFactory,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Check if current user is technician
            if (!currentUser.User!.IsTechnician())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // check car
            Car? gettingCar = await context
                .Cars.IgnoreQueryFilters()
                .Include(c => c.InspectionSchedules)
                .Where(c => !c.IsDeleted)
                .Where(c => c.Id == request.CarId)
                .FirstOrDefaultAsync(cancellationToken);
            if (gettingCar is null)
            {
                logger.LogError("Car not found: {CarId}", request.CarId);
                return Result.Error(ResponseMessages.CarNotFound);
            }

            // Check car must has any inprogress inspection schedule to continue
            if (
                !gettingCar.InspectionSchedules.Any(s =>
                    s.IsDeleted == false && s.Status == InspectionScheduleStatusEnum.InProgress
                )
            )
            {
                logger.LogError(
                    "Car {CarId} has no in-progress inspection schedule",
                    request.CarId
                );
                return Result.Error(
                    "Xe chưa có lịch kiểm định nào đang được tiến hành, không thể gán thiết bị GPS !"
                );
            }

            // check device
            GPSDevice? gettingDevice = await context
                .GPSDevices.IgnoreQueryFilters()
                .Include(d => d.GPS)
                .Include(d => d.Contract)
                .Where(d => d.OSBuildId == request.OSBuildId)
                .FirstOrDefaultAsync(cancellationToken);

            // add new device
            if (gettingDevice is not null)
            {
                // Check if device is used
                if (
                    gettingDevice.Contract != null
                    || gettingDevice.GPS != null
                    || gettingDevice.Status != DeviceStatusEnum.Available
                )
                {
                    logger.LogError(
                        "Device {OSBuildId} is already assigned to another car",
                        request.OSBuildId
                    );
                    return Result.Error(ResponseMessages.GPSDeviceIsNotAvailable);
                }
                gettingDevice.Name = request.DeviceName;
                gettingDevice.Status = DeviceStatusEnum.InUsed;
                gettingDevice.IsDeleted = false;
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
            newLocation.SRID = 4326;

            // Find car GPS association
            CarGPS? checkingCarGPS = await context
                .CarGPSes.IgnoreQueryFilters()
                .Where(c => c.CarId == request.CarId)
                .FirstOrDefaultAsync(cancellationToken);

            if (checkingCarGPS is not null)
            {
                checkingCarGPS.IsDeleted = false;
                await context.SaveChangesAsync(cancellationToken);
                logger.LogError(
                    "Device {OSBuildId} is already assigned to another car",
                    request.OSBuildId
                );
                return Result.Error(ResponseMessages.CarGPSIsExisted);
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
            return Result.Success(Response.FromEntity(gettingDevice), ResponseMessages.Created);
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
