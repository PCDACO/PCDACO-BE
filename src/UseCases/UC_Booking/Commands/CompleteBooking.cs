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
using UseCases.Services.PayOSService;

namespace UseCases.UC_Booking.Commands;

public sealed class CompleteBooking
{
    public sealed record Command(Guid BookingId) : IRequest<Result<Response>>;

    public sealed record Response(
        decimal TotalDistance,
        decimal ExcessDays,
        decimal ExcessFee,
        decimal BasePrice,
        decimal PlatformFee,
        decimal TotalAmount,
        string PaymentUrl,
        string QrCode
    );

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IEmailService emailService,
        IPaymentService paymentService,
        IBackgroundJobClient backgroundJobClient
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsDriver())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context
                .Bookings.Include(x => x.Status)
                .Include(x => x.Car)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.UserId != currentUser.User.Id)
                return Result.Forbidden(
                    "Bạn không có quyền thực hiện chức năng này với booking này!"
                );

            // Validate current status
            var invalidStatuses = new[]
            {
                BookingStatusEnum.Pending,
                BookingStatusEnum.Approved,
                BookingStatusEnum.Rejected,
                BookingStatusEnum.Completed,
                BookingStatusEnum.Cancelled,
                BookingStatusEnum.Expired
            };

            if (invalidStatuses.Contains(booking.Status.Name.ToEnum()))
            {
                return Result.Conflict(
                    $"Không thể phê duyệt booking ở trạng thái {booking.Status.Name}"
                );
            }

            var status = await context
                .BookingStatuses.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => EF.Functions.ILike(x.Name, BookingStatusEnum.Completed.ToString()),
                    cancellationToken
                );

            if (status == null)
                return Result.NotFound("Không tìm thấy trạng thái phù hợp");

            var lastTracking = await context
                .TripTrackings.Where(t => t.BookingId == request.BookingId)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            decimal totalDistance = lastTracking?.CumulativeDistance ?? 0;

            // Set actual return time and calculate excess fees
            booking.ActualReturnTime = DateTimeOffset.UtcNow;
            var (excessDays, excessFee) = CalculateExcessFee(booking);

            booking.StatusId = status.Id;
            booking.ExcessDay = excessDays;
            booking.ExcessDayFee = excessFee;
            booking.TotalAmount = booking.BasePrice + booking.PlatformFee + excessFee;
            booking.TotalDistance = totalDistance;

            var ownerAmount = booking.TotalAmount - booking.PlatformFee;

            // Create payment link
            var paymentResult = await paymentService.CreatePaymentLinkAsync(
                booking.Id,
                booking.TotalAmount,
                $"Thanh toan don hang",
                currentUser.User.Name
            );

            await context.SaveChangesAsync(cancellationToken);

            backgroundJobClient.Enqueue(
                () =>
                    SendEmail(
                        booking.User.Name,
                        booking.User.Email,
                        booking.Car.Owner.Name,
                        booking.Car.Owner.Email,
                        booking.Car.Model.Name,
                        totalDistance,
                        booking.BasePrice,
                        excessFee,
                        booking.PlatformFee,
                        booking.TotalAmount,
                        ownerAmount,
                        paymentResult.CheckoutUrl
                    )
            );

            return Result.Success(
                new Response(
                    TotalDistance: totalDistance / 1000, // Convert to kilometers
                    ExcessDays: excessDays,
                    ExcessFee: excessFee,
                    BasePrice: booking.BasePrice,
                    PlatformFee: booking.PlatformFee,
                    TotalAmount: booking.TotalAmount,
                    PaymentUrl: paymentResult.CheckoutUrl,
                    QrCode: paymentResult.QrCode
                )
            );
        }

        public async Task SendEmail(
            string driverName,
            string driverEmail,
            string ownerName,
            string ownerEmail,
            string carModel,
            decimal totalDistance,
            decimal basePrice,
            decimal excessFee,
            decimal platformFee,
            decimal totalAmount,
            decimal ownerEarning,
            string paymentUrl
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
                totalAmount,
                paymentUrl
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
            var dailyRate = booking.BasePrice / (decimal)plannedDays;

            // Apply penalty multiplier (e.g., 1.5x the daily rate for excess days)
            const decimal penaltyMultiplier = 1.5m;
            var excessFee = dailyRate * (decimal)excessDays * penaltyMultiplier;

            return ((decimal)excessDays, excessFee);
        }
    }
}
