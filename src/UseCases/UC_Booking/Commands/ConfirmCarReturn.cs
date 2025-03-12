using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Services.EmailService;

namespace UseCases.UC_Booking.Commands;

public sealed class ConfirmCarReturn
{
    public sealed record Command(Guid BookingId) : IRequest<Result<Response>>;

    public sealed record Response(
        decimal TotalDistance,
        decimal ExcessDays,
        decimal ExcessFee,
        decimal BasePrice,
        decimal PlatformFee,
        decimal TotalAmount
    );

    internal sealed class Handler(
        IAppDBContext context,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsOwner())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này!");

            var booking = await context
                .Bookings
                .Include(x => x.Car)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.Car.OwnerId != currentUser.User.Id)
                return Result.Forbidden("Bạn không có quyền phê duyệt booking cho xe này!");

            if (booking.Status != BookingStatusEnum.Completed)
                return Result.Conflict("Chỉ có thể xác nhận trả xe khi chuyến đi đã hoàn thành");

            booking.IsCarReturned = true;
            booking.ActualReturnTime = DateTimeOffset.UtcNow;

            var (excessDays, excessFee) = CalculateExcessFee(booking);

            booking.ExcessDay = excessDays;
            booking.ExcessDayFee = excessFee;
            booking.TotalAmount = booking.BasePrice + booking.PlatformFee + excessFee;
            booking.Car.Status = CarStatusEnum.Maintain;

            var ownerAmount = booking.TotalAmount - booking.PlatformFee;

            await context.SaveChangesAsync(cancellationToken);

            backgroundJobClient.Enqueue(
                () =>
                    SendEmail(
                        booking.User.Name,
                        booking.User.Email,
                        booking.Car.Owner.Name,
                        booking.Car.Owner.Email,
                        booking.Car.Model.Name,
                        booking.TotalDistance,
                        booking.BasePrice,
                        booking.PlatformFee,
                        booking.ExcessDayFee,
                        booking.TotalAmount,
                        ownerAmount
                    )
            );

            return Result.Success(
                new Response(
                    TotalDistance: booking.TotalDistance / 1000, // Convert to kilometers
                    ExcessDays: excessDays,
                    ExcessFee: excessFee,
                    BasePrice: booking.BasePrice,
                    PlatformFee: booking.PlatformFee,
                    TotalAmount: booking.TotalAmount
                )
            );
        }

        private static (decimal ExcessDays, decimal ExcessFee) CalculateExcessFee(Booking booking)
        {
            // If returned before or on time
            if (booking.ActualReturnTime <= booking.EndTime)
            {
                return (0, 0);
            }

            // Calculate excess days (round up to full days)
            var excessTimeSpan = booking.ActualReturnTime - booking.EndTime;
            var excessDays = Math.Ceiling(excessTimeSpan.TotalDays);

            // Calculate daily rate from the original booking
            var plannedDays = (booking.EndTime - booking.StartTime).TotalDays;
            var pricePerDay = booking.BasePrice / (decimal)plannedDays;

            const decimal penaltyMultiplier = 0.2m;
            var excessFee = pricePerDay * (decimal)excessDays * penaltyMultiplier;

            return ((decimal)excessDays, excessFee);
        }

        public async Task SendEmail(
            string driverName,
            string driverEmail,
            string ownerName,
            string ownerEmail,
            string carModel,
            decimal totalDistance,
            decimal basePrice,
            decimal platformFee,
            decimal excessFee,
            decimal totalAmount,
            decimal ownerEarning
        )
        {
            // Send email to driver
            var driverEmailTemplate = DriverCompleteBookingTemplate.Template(
                driverName,
                carModel,
                totalDistance,
                basePrice,
                excessFee,
                platformFee,
                totalAmount
            );

            await emailService.SendEmailAsync(
                driverEmail,
                "Chuyến đi của bạn đã hoàn thành",
                driverEmailTemplate
            );

            // Send email to driver
            var ownerEmailTemplate = OwnerBookingCompletedTemplate.Template(
                ownerName,
                driverName,
                carModel,
                totalDistance,
                basePrice,
                excessFee,
                platformFee,
                totalAmount,
                ownerEarning
            );

            await emailService.SendEmailAsync(
                ownerEmail,
                "Thông Báo Hoàn Thành Chuyến Đi",
                ownerEmailTemplate
            );
        }
    }
}