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
        decimal UnusedDays,
        decimal RefundAmount,
        decimal ExcessDays,
        decimal ExcessFee,
        decimal BasePrice,
        decimal PlatformFee,
        decimal FinalAmount
    );

    internal sealed class Handler(
        IAppDBContext context,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        private const decimal EARLY_RETURN_REFUND_PERCENTAGE = 0.3m; // 30% refund for unused days
        private const decimal LATE_RETURN_PENALTY_MULTIPLIER = 1.2m; // 120% of daily rate for late days

        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsOwner())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này!");

            var booking = await context
                .Bookings.Include(x => x.Car)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.Car.OwnerId != currentUser.User.Id)
                return Result.Forbidden("Bạn không có quyền phê duyệt booking cho xe này!");

            if (booking.Status != BookingStatusEnum.Completed)
                return Result.Conflict("Chỉ có thể xác nhận trả xe khi chuyến đi đã hoàn thành");

            booking.IsCarReturned = true;
            booking.ActualReturnTime = DateTimeOffset.UtcNow;

            var totalBookingDays = (booking.EndTime - booking.StartTime).Days;
            var actualDays = (booking.ActualReturnTime - booking.StartTime).Days;
            var dailyRate = booking.BasePrice / totalBookingDays;

            decimal refundAmount = 0;
            decimal excessDays = 0;
            decimal excessFee = 0;

            // Early Return Case
            if (actualDays < totalBookingDays)
            {
                var unusedDays = totalBookingDays - actualDays;
                refundAmount = dailyRate * unusedDays * EARLY_RETURN_REFUND_PERCENTAGE;

                if (booking.IsPaid)
                {
                    booking.IsRefund = true;
                    booking.RefundAmount = refundAmount;
                }
            }
            // Late Return Case
            else if (actualDays > totalBookingDays)
            {
                excessDays = actualDays - totalBookingDays;
                excessFee = dailyRate * excessDays * LATE_RETURN_PENALTY_MULTIPLIER;

                booking.ExcessDay = excessDays;
                booking.ExcessDayFee = excessFee;
            }

            // Calculate final amount
            var finalAmount = booking.BasePrice + booking.PlatformFee + excessFee - refundAmount;
            booking.TotalAmount = finalAmount;
            booking.Car.Status = CarStatusEnum.Maintain;

            await context.SaveChangesAsync(cancellationToken);

            // Schedule maintenance period check after 3 days
            backgroundJobClient.Schedule(
                () => CheckMaintenancePeriod(booking.Car.Id),
                TimeSpan.FromDays(3)
            );

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
                        excessFee,
                        refundAmount,
                        finalAmount
                    )
            );

            return Result.Success(
                new Response(
                    TotalDistance: booking.TotalDistance / 1000, // Convert to kilometers
                    UnusedDays: actualDays < totalBookingDays ? totalBookingDays - actualDays : 0,
                    RefundAmount: refundAmount,
                    ExcessDays: excessDays,
                    ExcessFee: excessFee,
                    BasePrice: booking.BasePrice,
                    PlatformFee: booking.PlatformFee,
                    FinalAmount: finalAmount
                )
            );
        }

        private async Task CheckMaintenancePeriod(Guid carId)
        {
            var car = await context.Cars.FindAsync(carId);
            if (car != null && car.Status == CarStatusEnum.Maintain)
            {
                car.Status = CarStatusEnum.Available;
                await context.SaveChangesAsync(CancellationToken.None);
            }
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
            decimal refundAmount,
            decimal finalAmount
        )
        {
            string subject;
            if (excessFee > 0)
                subject = "Chuyến đi của bạn đã hoàn thành - Có phí phạt trả xe muộn";
            else if (refundAmount > 0)
                subject = "Chuyến đi của bạn đã hoàn thành - Có hoàn tiền cho ngày không sử dụng";
            else
                subject = "Chuyến đi của bạn đã hoàn thành";

            // Send email to driver
            var driverEmailTemplate = DriverCompleteBookingTemplate.Template(
                driverName,
                carModel,
                totalDistance,
                basePrice,
                excessFee,
                platformFee,
                finalAmount
            );

            await emailService.SendEmailAsync(driverEmail, subject, driverEmailTemplate);

            // Send email to owner
            var ownerEmailTemplate = OwnerBookingCompletedTemplate.Template(
                ownerName,
                driverName,
                carModel,
                totalDistance,
                basePrice,
                excessFee,
                platformFee,
                finalAmount,
                finalAmount - platformFee
            );

            await emailService.SendEmailAsync(
                ownerEmail,
                "Thông Báo Hoàn Thành Chuyến Đi",
                ownerEmailTemplate
            );
        }
    }
}
