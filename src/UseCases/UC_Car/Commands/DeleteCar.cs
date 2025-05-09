using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Commands;

public sealed class DeleteCar
{
    public record Command(Guid Id) : IRequest<Result>;

    public class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (currentUser.User!.IsAdmin())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            Car? deletingCar = await context
                .Cars.Where(c => c.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (deletingCar is null)
                return Result.NotFound(ResponseMessages.CarNotFound);
            if (deletingCar.OwnerId != currentUser.User.Id)
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            //Check if car belongs to any active bookings then cannot delete
            var activeBookings = context
                .Bookings.AsNoTracking()
                .Where(b =>
                    b.CarId == deletingCar.Id
                    && (
                        b.Status == BookingStatusEnum.Pending
                        || b.Status == BookingStatusEnum.Ongoing
                        || b.Status == BookingStatusEnum.ReadyForPickup
                        || b.Status == BookingStatusEnum.Approved
                    )
                );
            if (activeBookings.Any())
                return Result.Error("Xe đang có đơn thuê xe không thể xóa!");
            // Check if car has any active inspection schedule can not delete
            var activeInspectionSchedules = context
                .InspectionSchedules.AsNoTracking()
                .Where(i =>
                    i.CarId == deletingCar.Id
                    && (
                        i.Status == InspectionScheduleStatusEnum.Pending
                        || i.Status == InspectionScheduleStatusEnum.InProgress
                        || i.Status == InspectionScheduleStatusEnum.Signed
                    )
                );
            if (activeInspectionSchedules.Any())
                return Result.Error("Xe đang có lịch kiểm định không thể xóa!");
            // Check if any gps device is attached to the car can not delete
            var carGPS = context.CarGPSes.AsNoTracking().Where(g => g.CarId == deletingCar.Id);
            if (carGPS.Any())
                return Result.Error("Vui lòng gỡ thiết bị gps trước khi xóa xe!");
            // Soft delete image car
            await context
                .ImageCars.Where(ic => ic.CarId == deletingCar.Id)
                .ExecuteUpdateAsync(
                    ic =>
                        ic.SetProperty(i => i.DeletedAt, DateTime.UtcNow)
                            .SetProperty(i => i.IsDeleted, true)
                            .SetProperty(i => i.UpdatedAt, DateTime.UtcNow),
                    cancellationToken
                );
            // Soft delete car amenities
            await context
                .CarAmenities.Where(ca => ca.CarId == deletingCar.Id)
                .ExecuteUpdateAsync(
                    ca =>
                        ca.SetProperty(i => i.DeletedAt, DateTime.UtcNow)
                            .SetProperty(i => i.IsDeleted, true)
                            .SetProperty(i => i.UpdatedAt, DateTime.UtcNow),
                    cancellationToken
                );
            // Soft delete car statistics
            await context
                .CarStatistics.Where(cs => cs.CarId == deletingCar.Id)
                .ExecuteUpdateAsync(
                    cs =>
                        cs.SetProperty(i => i.DeletedAt, DateTime.UtcNow)
                            .SetProperty(i => i.IsDeleted, true)
                            .SetProperty(i => i.UpdatedAt, DateTime.UtcNow),
                    cancellationToken
                );
            deletingCar.Delete();
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage(ResponseMessages.Deleted);
        }
    }
}
