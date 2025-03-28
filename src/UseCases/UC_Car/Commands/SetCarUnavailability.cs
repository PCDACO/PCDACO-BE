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
    public record Command(Guid CarId, List<DateTimeOffset> Dates, bool IsAvailable = false)
        : IRequest<Result>;

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

            // Normalize dates to midnight for consistent comparison
            var normalizedDates = request
                .Dates.Select(d => new DateTimeOffset(d.Date, d.Offset))
                .ToList();

            // Check for existing bookings
            var dateToCheck = normalizedDates.ToHashSet();
            var existingBookingDate = context
                .Bookings.Where(b =>
                    b.CarId == request.CarId
                    && !b.IsDeleted
                    && b.Status != Domain.Enums.BookingStatusEnum.Cancelled
                )
                .AsEnumerable()
                .Where(b =>
                    dateToCheck.Any(d =>
                        b.StartTime.UtcDateTime.Date <= d.UtcDateTime.Date
                        && b.EndTime.UtcDateTime.Date >= d.UtcDateTime.Date
                    )
                )
                .Select(b => b.StartTime)
                .FirstOrDefault();

            if (existingBookingDate != default)
                return Result.Conflict(
                    $"Không thể thay đổi trạng thái vì ngày {existingBookingDate} đã có đơn đặt xe"
                );

            // Get all existing availability records
            var datesToCheck = normalizedDates.Select(d => d.UtcDateTime).ToHashSet();

            // Get existing availabilities
            var existingAvailabilities = await context
                .CarAvailabilities.Where(ca =>
                    ca.CarId == request.CarId
                    && !ca.IsDeleted
                    && datesToCheck.Contains(ca.Date.UtcDateTime.Date)
                )
                .ToDictionaryAsync(ca => ca.Date.Date, ca => ca, cancellationToken);

            // Prepare records to add or update
            var toAdd = new List<CarAvailability>();

            foreach (var date in normalizedDates)
            {
                if (existingAvailabilities.TryGetValue(date.Date, out var existing))
                {
                    // Update existing record
                    existing.IsAvailable = request.IsAvailable;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    // Create new availability record
                    toAdd.Add(
                        new CarAvailability
                        {
                            CarId = request.CarId,
                            Date = date,
                            IsAvailable = request.IsAvailable,
                        }
                    );
                }
            }

            // Add new records if any
            if (toAdd.Count > 0)
            {
                await context.CarAvailabilities.AddRangeAsync(toAdd, cancellationToken);
            }

            // Save all changes in one transaction
            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage(ResponseMessages.Updated);
        }
    }
}
