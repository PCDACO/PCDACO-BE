using Ardalis.Result;
using Domain.Constants;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_GPSDevice.Commands;

public class UnassignGPSDeviceForCar
{
    public record Command(Guid GPSDeviceId) : IRequest<Result>;

    public class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check if current user is admin or technician
            if (!currentUser.User!.IsAdmin() && !currentUser.User.IsTechnician())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Check if the GPS device exists and is not deleted
            var device = await context
                .GPSDevices.Where(d => d.Id == request.GPSDeviceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (device is null)
                return Result.Error(ResponseMessages.GPSDeviceNotFound);

            // Find the car GPS association for this device
            var carGPS = await context
                .CarGPSes.IgnoreQueryFilters()
                .Include(c => c.Car)
                .ThenInclude(c => c.InspectionSchedules)
                .Where(c => c.DeviceId == request.GPSDeviceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (carGPS is null)
                return Result.NotFound("Thiết bị GPS không được gán cho xe nào");

            if (
                !carGPS.Car.InspectionSchedules.Any(i =>
                    i.Status == InspectionScheduleStatusEnum.InProgress
                    && i.Type == InspectionScheduleType.ChangeGPS
                )
            )
                return Result.Error("Xe không có lịch đổi thiết bị gps nào đang diễn ra");

            // Check if the car has any active bookings
            var hasActiveBookings = await context.Bookings.AnyAsync(
                b =>
                    b.CarId == carGPS.Car.Id
                    && (
                        b.Status == BookingStatusEnum.Pending
                        || b.Status == BookingStatusEnum.Ongoing
                        || b.Status == BookingStatusEnum.ReadyForPickup
                        || b.Status == BookingStatusEnum.Approved
                    ),
                cancellationToken
            );

            if (hasActiveBookings)
                return Result.Error("Xe đang có lịch đặt, không thể gỡ thiết bị GPS");

            // Update device status to Available
            device.Status = DeviceStatusEnum.Available;
            device.UpdatedAt = DateTimeOffset.UtcNow;

            // Update the GPSDeviceId field of the contract to null
            var contract = await context
                .CarContracts.Where(c => c.GPSDeviceId == request.GPSDeviceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (contract is not null)
            {
                contract.GPSDeviceId = null;
                contract.UpdatedAt = DateTimeOffset.UtcNow;
            }

            // Remove CarGPS
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
