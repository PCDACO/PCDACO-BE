using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Services.EmailService;

namespace UseCases.UC_Booking.Commands;

public sealed class CompleteBooking
{
    public sealed record Command(Guid BookingId, decimal ReturnLatitude, decimal ReturnLongitude)
        : IRequest<Result<Response>>;

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
        IBackgroundJobClient backgroundJobClient,
        IEmailService emailService,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        private static readonly GeometryFactory GeometryFactory =
            NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        private const int SRID = 4326; // WGS84 coordinate system
        private const decimal MAX_ALLOWED_DISTANCE_METERS = 100;
        private const decimal EARLY_RETURN_REFUND_PERCENTAGE = 0.5m; // 50% refund for unused days if less than half of total days
        private const decimal LATE_RETURN_PENALTY_MULTIPLIER = 1.2m; // 120% of daily rate for late days

        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsDriver())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context
                .Bookings.Include(x => x.Car)
                .ThenInclude(x => x.Model)
                .Include(x => x.Car)
                .ThenInclude(x => x.Owner)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.UserId != currentUser.User.Id)
                return Result.Forbidden(
                    "Bạn không có quyền thực hiện chức năng này với booking này!"
                );

            // Validate current status
            if (booking.Status != BookingStatusEnum.Ongoing)
            {
                return Result.Conflict(
                    $"Không thể phê duyệt booking ở trạng thái " + booking.Status.ToString()
                );
            }

            // Create return location point
            var returnLocation = GeometryFactory.CreatePoint(
                new Coordinate((double)request.ReturnLongitude, (double)request.ReturnLatitude)
            );
            returnLocation.SRID = SRID;

            // Calculate distance from car's pickup location
            var distanceInMeters =
                (decimal)booking.Car.PickupLocation.Distance(returnLocation) * 111320m; // Convert degrees to meters

            if (distanceInMeters > MAX_ALLOWED_DISTANCE_METERS)
            {
                return Result.Error(
                    $"Bạn phải trả xe tại địa điểm đã đón xe: {booking.Car.PickupAddress}. "
                        + $"Vui lòng di chuyển đến trong phạm vi {MAX_ALLOWED_DISTANCE_METERS} mét so với vị trí đón xe!"
                );
            }

            var lastTracking = await context
                .TripTrackings.Where(t => t.BookingId == request.BookingId)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            decimal totalDistance = lastTracking?.CumulativeDistance ?? 0;

            // Calculate actual rental duration
            var actualReturnTime = DateTimeOffset.UtcNow;
            var totalBookingDays = Math.Ceiling(
                (decimal)(booking.EndTime - booking.StartTime).TotalDays
            );
            var actualDays = Math.Ceiling(
                (decimal)(actualReturnTime - booking.StartTime).TotalDays
            );
            var dailyRate = booking.BasePrice / totalBookingDays;

            decimal refundAmount = 0;
            decimal excessDays = 0;
            decimal excessFee = 0;
            decimal unusedDays = 0;

            // Early Return Case
            if (actualDays < (totalBookingDays / 2))
            {
                unusedDays = totalBookingDays - actualDays;
                refundAmount = dailyRate * unusedDays * EARLY_RETURN_REFUND_PERCENTAGE;
            }
            // Late Return Case
            else if (actualDays > totalBookingDays)
            {
                excessDays = actualDays - totalBookingDays;
                excessFee = dailyRate * excessDays * LATE_RETURN_PENALTY_MULTIPLIER;
            }

            // Calculate final amount
            var finalAmount = booking.BasePrice + booking.PlatformFee + excessFee - refundAmount;

            // Update booking
            booking.Status = BookingStatusEnum.Completed;
            booking.TotalDistance = totalDistance;
            booking.ActualReturnTime = actualReturnTime;
            booking.ExcessDay = excessDays;
            booking.ExcessDayFee = excessFee;
            booking.TotalAmount = finalAmount;
            booking.UpdatedAt = DateTimeOffset.UtcNow;

            if (refundAmount > 0)
            {
                booking.IsRefund = true;
                booking.RefundAmount = refundAmount;
            }

            await context.SaveChangesAsync(cancellationToken);

            // Send notification emails
            backgroundJobClient.Enqueue(
                () =>
                    SendEmail(
                        booking.User.Name,
                        booking.Car.Owner.Name,
                        booking.User.Email,
                        booking.Car.Owner.Email,
                        booking.Car.Model.Name,
                        totalDistance,
                        booking.BasePrice,
                        excessFee,
                        booking.PlatformFee,
                        finalAmount
                    )
            );

            return Result.Success(
                new Response(
                    TotalDistance: totalDistance / 1000, // Convert to kilometers
                    UnusedDays: unusedDays,
                    RefundAmount: refundAmount,
                    ExcessDays: excessDays,
                    ExcessFee: excessFee,
                    BasePrice: booking.BasePrice,
                    PlatformFee: booking.PlatformFee,
                    FinalAmount: finalAmount
                )
            );
        }

        public async Task SendEmail(
            string driverName,
            string ownerName,
            string driverEmail,
            string ownerEmail,
            string carModelName,
            decimal totalDistance,
            decimal basePrice,
            decimal excessDayFee,
            decimal platformFee,
            decimal totalAmount
        )
        {
            // Send email to driver
            var driverEmailTemplate = DriverCompleteBookingTemplate.Template(
                driverName,
                carModelName,
                totalDistance / 1000, // Convert to kilometers
                basePrice,
                excessDayFee,
                platformFee,
                totalAmount
            );

            await emailService.SendEmailAsync(
                driverEmail,
                "Thông Báo Hoàn Thành Chuyến Đi",
                driverEmailTemplate
            );

            // Send email to owner
            var ownerEmailTemplate = OwnerBookingCompletedTemplate.Template(
                ownerName,
                driverName,
                carModelName,
                totalDistance / 1000, // Convert to kilometers
                basePrice,
                excessDayFee,
                platformFee,
                totalAmount,
                totalAmount - platformFee
            );

            await emailService.SendEmailAsync(
                ownerEmail,
                "Thông Báo Hoàn Thành Chuyến Đi",
                ownerEmailTemplate
            );
        }
    }
}
