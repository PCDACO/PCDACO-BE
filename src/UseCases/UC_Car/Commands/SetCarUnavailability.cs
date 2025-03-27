using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Commands;

public class SetCarUnavailability
{
    public record Command(Guid CarId, DateTimeOffset Date, bool IsAvailable = false)
        : IRequest<Result>;

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Find the car
            var car = await context.Cars.FirstOrDefaultAsync(
                c => c.Id == request.CarId && !c.IsDeleted,
                cancellationToken
            );

            if (car == null)
                return Result.Error(ResponseMessages.CarNotFound);

            // Check if user is the car owner
            if (car.OwnerId != currentUser.User!.Id)
                return Result.Error(ResponseMessages.ForbiddenAudit);

            // Check if there's an existing booking for this date
            var existingBookings = await context.Bookings.AnyAsync(
                b =>
                    b.CarId == request.CarId
                    && b.StartTime.Date <= request.Date.Date
                    && b.EndTime.Date >= request.Date.Date
                    && b.Status != Domain.Enums.BookingStatusEnum.Cancelled,
                cancellationToken
            );

            if (existingBookings)
                return Result.Conflict(
                    "Không thể thay đổi trạng thái ngày này vì đã có đơn đặt xe"
                );

            // Find existing availability record for the date if any
            var existingAvailability = await context.CarAvailabilities.FirstOrDefaultAsync(
                ca =>
                    ca.CarId == request.CarId && ca.Date.Date == request.Date.Date && !ca.IsDeleted,
                cancellationToken
            );

            if (existingAvailability != null)
            {
                // Update existing record
                existingAvailability.IsAvailable = request.IsAvailable;
                existingAvailability.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                // Create new availability record
                var carAvailability = new CarAvailability
                {
                    CarId = request.CarId,
                    Date = request.Date.Date,
                    IsAvailable = request.IsAvailable,
                };

                await context.CarAvailabilities.AddAsync(carAvailability, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage(ResponseMessages.Updated);
        }
    }
}
