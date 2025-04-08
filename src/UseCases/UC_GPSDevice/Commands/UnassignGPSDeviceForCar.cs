using Ardalis.Result;
using Domain.Constants;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.UC_GPSDevice.Commands;

public class UnassignGPSDeviceForCar
{
    public record Command(Guid GPSDeviceId) : IRequest<Result>;

    public class Handler(IAppDBContext context) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check if the GPS device exists and is not deleted
            var device = await context
                .GPSDevices.Where(d => !d.IsDeleted)
                .Where(d => d.Id == request.GPSDeviceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (device is null)
                return Result.Error(ResponseMessages.GPSDeviceNotFound);

            // Find the car GPS association for this device
            var carGPS = await context
                .CarGPSes.IgnoreQueryFilters()
                .Include(c => c.Car)
                .Where(c => c.DeviceId == request.GPSDeviceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (carGPS is null)
                return Result.NotFound("Thiết bị GPS không được gán cho xe nào");

            // Check if the car is in pending status or deleted
            if (!carGPS.Car.IsDeleted && carGPS.Car.Status != CarStatusEnum.Pending)
                return Result.Conflict(
                    "Chỉ có thể gỡ thiết bị GPS khỏi xe đã bị xóa hoặc đang trong trạng thái chờ"
                );

            // Update device status to Available
            device.Status = DeviceStatusEnum.Available;
            device.UpdatedAt = DateTimeOffset.UtcNow;

            // remove car gps association in the database
            context.CarGPSes.Remove(carGPS);

            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Gỡ thiết bị GPS khỏi xe thành công");
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.GPSDeviceId)
                .NotEmpty()
                .WithMessage("ID thiết bị GPS không được để trống");
        }
    }
}
