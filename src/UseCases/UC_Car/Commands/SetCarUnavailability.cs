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
    public record Command(Guid CarId, List<DateTimeOffset> Dates) : IRequest<Result>;

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check if dates are provided
            if (request.Dates == null || !request.Dates.Any())
                return Result.Error("Ngày không hợp lệ");

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

            // Check for existing bookings
            var existingBookingDate = context
                .Bookings.Where(b =>
                    b.CarId == request.CarId
                    && !b.IsDeleted
                    && (
                        b.Status == Domain.Enums.BookingStatusEnum.Ongoing
                        || b.Status == Domain.Enums.BookingStatusEnum.Pending
                        || b.Status == Domain.Enums.BookingStatusEnum.ReadyForPickup
                        || b.Status == Domain.Enums.BookingStatusEnum.Approved
                    )
                    && request.Dates.Any(d =>
                        b.StartTime.Date <= d.Date && b.EndTime.Date >= d.Date
                    )
                )
                .Select(b => b.StartTime)
                .FirstOrDefault();

            if (existingBookingDate != default)
                return Result.Conflict(
                    $"Không thể thay đổi trạng thái vì ngày {existingBookingDate} đã có đơn đặt xe"
                );

            // Get existing car availabilities for this car
            var existingAvailabilities = context.CarAvailabilities.Where(ca =>
                ca.CarId == request.CarId && !ca.IsDeleted
            );

            // Find dates that already exist in the database
            var existingDates = existingAvailabilities.Select(ca => ca.Date.Date).ToHashSet();

            // Add new dates that don't exist yet
            foreach (var date in request.Dates)
            {
                if (!existingDates.Contains(date.Date))
                {
                    context.CarAvailabilities.Add(
                        new CarAvailability
                        {
                            CarId = request.CarId,
                            Date = date,
                            IsAvailable = false,
                        }
                    );
                }
            }

            // Soft delete dates that are not in request.Dates
            var requestDateSet = request.Dates.Select(d => d.Date).ToHashSet();
            foreach (var availability in existingAvailabilities)
            {
                if (!requestDateSet.Contains(availability.Date.Date))
                {
                    availability.IsDeleted = true;
                }
            }

            // Save all changes in one transaction
            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage(ResponseMessages.Updated);
        }
    }
}
