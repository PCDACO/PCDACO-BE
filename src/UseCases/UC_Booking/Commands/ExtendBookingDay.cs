using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Services.EmailService;

namespace UseCases.UC_Booking.Commands;

public class ExtendBookingDay
{
    const decimal PLATFORM_FEE_RATE = 0.1m;

    public sealed record Command(
        Guid BookingId,
        DateTimeOffset NewStartTime,
        DateTimeOffset NewEndTime
    ) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid BookingId,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
        decimal? AdditionalAmount
    );

    internal sealed class Handler(
        IAppDBContext context,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        private const int PAYMENT_TIMEOUT_MINUTES = 15;

        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsDriver() && !currentUser.User!.IsOwner())
                return Result<Response>.Forbidden("Bạn không có quyền gia hạn thời gian đặt xe");

            var booking = await context
                .Bookings.Include(b => b.Car)
                .Include(b => b.User)
                .Include(b => b.Car.Owner)
                .Include(b => b.Car.Model)
                .Include(b => b.Contract)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking is null)
                return Result<Response>.NotFound("Không tìm thấy booking");

            if (booking.UserId != currentUser.User.Id)
                return Result<Response>.Forbidden("Bạn không có quyền thao tác với booking này");

            // Handle based on booking status
            switch (booking.Status)
            {
                case BookingStatusEnum.Pending:
                    return await HandlePendingBookingChange(
                        booking,
                        request.NewStartTime,
                        request.NewEndTime,
                        cancellationToken
                    );

                case BookingStatusEnum.Approved:
                case BookingStatusEnum.ReadyForPickup:

                    return await HandlePreStartBookingChange(
                        booking,
                        request.NewStartTime,
                        cancellationToken
                    );

                case BookingStatusEnum.Ongoing:
                    if (!booking.IsPaid)
                        return Result<Response>.Error("Booking chưa được thanh toán");

                    return await HandleOngoingBookingExtension(
                        booking,
                        request.NewEndTime,
                        cancellationToken
                    );

                default:
                    return Result<Response>.Error("Không thể thay đổi thời gian cho booking này");
            }
        }

        private async Task<Result<Response>> HandlePendingBookingChange(
            Booking booking,
            DateTimeOffset newStartDate,
            DateTimeOffset newEndDate,
            CancellationToken cancellationToken
        )
        {
            // Store old dates for email
            var oldStartTime = booking.StartTime;
            var oldEndTime = booking.EndTime;

            // Check for unavailable dates
            var hasUnavailableDates = await context
                .CarAvailabilities.Where(ca =>
                    ca.CarId == booking.CarId
                    && !ca.IsAvailable
                    && DateOnly.FromDateTime(ca.Date.Date)
                        >= DateOnly.FromDateTime(newStartDate.Date)
                    && DateOnly.FromDateTime(ca.Date.Date) <= DateOnly.FromDateTime(newEndDate.Date)
                )
                .AnyAsync(cancellationToken);

            if (hasUnavailableDates)
                return Result<Response>.Error("Xe không khả dụng trong khoảng thời gian này");

            // Check for booking conflicts
            var hasConflict = await context
                .Bookings.AsNoTracking()
                .AnyAsync(
                    b =>
                        b.CarId == booking.CarId
                        && b.Id != booking.Id
                        && (
                            b.Status == BookingStatusEnum.Approved
                            || b.Status == BookingStatusEnum.ReadyForPickup
                            || b.Status == BookingStatusEnum.Ongoing
                        )
                        && b.StartTime.Date <= newEndDate.Date
                        && b.EndTime.Date >= newStartDate.Date,
                    cancellationToken
                );

            if (hasConflict)
                return Result<Response>.Error("Thời gian này đã có booking khác");

            // Recalculate total amount based on new duration
            var newTotalBookingDays = Math.Ceiling((newEndDate - newStartDate).TotalDays);
            var newBasePrice = booking.Car.Price * (decimal)newTotalBookingDays;
            var newPlatformFee = newBasePrice * PLATFORM_FEE_RATE;
            var newTotalAmount = newBasePrice + newPlatformFee;

            booking.BasePrice = newBasePrice;
            booking.PlatformFee = newPlatformFee;
            booking.TotalAmount = newTotalAmount;

            // Update booking dates
            booking.StartTime = newStartDate;
            booking.EndTime = newEndDate;
            booking.ActualReturnTime = newEndDate;
            booking.UpdatedAt = DateTimeOffset.UtcNow;

            // Update contract dates
            booking.Contract.StartDate = newStartDate;
            booking.Contract.EndDate = newEndDate;
            booking.Contract.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            // Send notification emails
            backgroundJobClient.Enqueue(
                () =>
                    SendExtensionSuccessEmail(
                        booking.User.Name,
                        booking.User.Email,
                        booking.Car.Model.Name,
                        oldStartTime,
                        oldEndTime,
                        newStartDate,
                        newEndDate,
                        null // Explicitly pass null for the optional parameter
                    )
            );

            backgroundJobClient.Enqueue(
                () =>
                    SendExtensionSuccessEmail(
                        booking.Car.Owner.Name,
                        booking.Car.Owner.Email,
                        booking.Car.Model.Name,
                        oldStartTime,
                        oldEndTime,
                        newStartDate,
                        newEndDate,
                        null
                    )
            );

            return Result<Response>.Success(
                new Response(booking.Id, booking.StartTime, booking.EndTime, null),
                "Điều chỉnh lịch đặt xe thành công"
            );
        }

        private async Task<Result<Response>> HandlePreStartBookingChange(
            Booking booking,
            DateTimeOffset newStartTime,
            CancellationToken cancellationToken
        )
        {
            // Store old dates before updating
            var oldStartTime = booking.StartTime;
            var oldEndTime = booking.EndTime;
            var oldDuration = Math.Ceiling((oldEndTime - oldStartTime).TotalDays);

            // Calculate new end date based on old duration
            var newEndTime = newStartTime.AddDays(oldDuration);

            // Check for unavailable dates
            var hasUnavailableDates = await context
                .CarAvailabilities.Where(ca =>
                    ca.CarId == booking.CarId
                    && !ca.IsAvailable
                    && DateOnly.FromDateTime(ca.Date.Date)
                        >= DateOnly.FromDateTime(newStartTime.Date)
                    && DateOnly.FromDateTime(ca.Date.Date) <= DateOnly.FromDateTime(newEndTime.Date)
                )
                .AnyAsync(cancellationToken);

            if (hasUnavailableDates)
                return Result<Response>.Error("Xe không khả dụng trong khoảng thời gian này");

            // Check for conflicts
            var hasConflict = await context.Bookings.AnyAsync(
                b =>
                    b.CarId == booking.CarId
                    && b.Id != booking.Id
                    && (
                        b.Status == BookingStatusEnum.Approved
                        || b.Status == BookingStatusEnum.ReadyForPickup
                        || b.Status == BookingStatusEnum.Ongoing
                    )
                    && (
                        (newStartTime >= b.StartTime && newStartTime <= b.EndTime)
                        || (newEndTime >= b.StartTime && newEndTime <= b.EndTime)
                        || (newStartTime <= b.StartTime && newEndTime >= b.EndTime)
                    ),
                cancellationToken
            );

            if (hasConflict)
                return Result<Response>.Error("Thời gian này đã có booking khác");

            if (newStartTime > oldStartTime)
            {
                // Check if the new start time is more than 24 hours away
                if ((newStartTime - DateTimeOffset.UtcNow).TotalHours > 24)
                {
                    // Delete related InspectionPhotos before deleting CarInspections
                    var inspectionsToDelete = await context
                        .CarInspections.Include(ci => ci.Photos)
                        .Where(ci =>
                            ci.BookingId == booking.Id && ci.Type == InspectionType.PreBooking
                        )
                        .ToListAsync(cancellationToken);

                    foreach (var inspection in inspectionsToDelete)
                    {
                        // Delete related InspectionPhotos
                        context.InspectionPhotos.RemoveRange(inspection.Photos);
                        context.CarInspections.Remove(inspection);
                    }
                }

                booking.Status = BookingStatusEnum.Approved; // Update status if necessary
            }
            booking.StartTime = newStartTime;
            booking.EndTime = newEndTime;
            booking.ActualReturnTime = newEndTime;
            booking.UpdatedAt = DateTimeOffset.UtcNow;

            // Update contract
            booking.Contract.StartDate = newStartTime;
            booking.Contract.EndDate = newEndTime;
            booking.Contract.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            // Send notification emails
            backgroundJobClient.Enqueue(
                () =>
                    SendExtensionSuccessEmail(
                        booking.User.Name,
                        booking.User.Email,
                        booking.Car.Model.Name,
                        oldStartTime,
                        oldEndTime,
                        newStartTime,
                        newEndTime,
                        null
                    )
            );

            backgroundJobClient.Enqueue(
                () =>
                    SendExtensionSuccessEmail(
                        booking.Car.Owner.Name,
                        booking.Car.Owner.Email,
                        booking.Car.Model.Name,
                        oldStartTime,
                        oldEndTime,
                        newStartTime,
                        newEndTime,
                        null
                    )
            );

            return Result<Response>.Success(
                new Response(booking.Id, booking.StartTime, booking.EndTime, null),
                "Điểu chỉnh lịch thành công"
            );
        }

        private async Task<Result<Response>> HandleOngoingBookingExtension(
            Booking booking,
            DateTimeOffset newEndDate,
            CancellationToken cancellationToken
        )
        {
            if (newEndDate <= booking.EndTime)
                return Result<Response>.Error("Thời gian gia hạn phải lớn hơn thời gian hiện tại");

            // Check for unavailable dates specified by owner
            var endDate = DateOnly.FromDateTime(newEndDate.Date);
            var startDate = DateOnly.FromDateTime(booking.EndTime.Date);

            var hasUnavailableDates = await context
                .CarAvailabilities.Where(ca =>
                    ca.CarId == booking.CarId
                    && !ca.IsAvailable
                    && DateOnly.FromDateTime(ca.Date.Date) >= startDate
                    && DateOnly.FromDateTime(ca.Date.Date) <= endDate
                )
                .AnyAsync(cancellationToken);

            if (hasUnavailableDates)
                return Result<Response>.Error("Xe không khả dụng trong khoảng thời gian này");

            // Check for booking conflicts
            var hasConflict = await context
                .Bookings.AsNoTracking()
                .AnyAsync(
                    b =>
                        b.CarId == booking.CarId
                        && b.Id != booking.Id
                        && (
                            b.Status == BookingStatusEnum.Approved
                            || b.Status == BookingStatusEnum.ReadyForPickup
                            || b.Status == BookingStatusEnum.Ongoing
                        )
                        && b.StartTime.Date <= newEndDate.Date
                        && b.EndTime.Date >= booking.EndTime.Date,
                    cancellationToken
                );

            if (hasConflict)
                return Result<Response>.Error("Thời gian này đã có booking khác");

            // Calculate extension details
            var additionalDays = Math.Ceiling((newEndDate - booking.EndTime).TotalDays);
            var newBasePrice = booking.Car.Price * (decimal)additionalDays;
            var newPlatformFee = newBasePrice * PLATFORM_FEE_RATE;
            var newTotalAmount = newBasePrice + newPlatformFee;

            // Store old values for potential revert
            var oldEndDate = booking.EndTime;
            var oldBasePrice = booking.BasePrice;
            var oldPlatformFee = booking.PlatformFee;
            var oldTotalAmount = booking.TotalAmount;

            // Update booking
            booking.EndTime = newEndDate;
            booking.ActualReturnTime = newEndDate;
            booking.UpdatedAt = DateTimeOffset.UtcNow;
            booking.Note = "Người dùng gia hạn ngày trả xe";
            booking.BasePrice += newBasePrice;
            booking.PlatformFee += newPlatformFee;
            booking.TotalAmount += newTotalAmount;
            booking.ExtensionAmount = newTotalAmount;
            booking.IsExtensionPaid = false;

            // Update contract
            booking.Contract.EndDate = newEndDate;
            booking.Contract.UpdatedAt = DateTimeOffset.UtcNow;

            // Schedule job to check payment and revert if not paid
            backgroundJobClient.Schedule(
                () =>
                    RevertExtensionIfNotPaid(
                        booking.Id,
                        oldEndDate,
                        oldBasePrice,
                        oldPlatformFee,
                        oldTotalAmount
                    ),
                TimeSpan.FromMinutes(PAYMENT_TIMEOUT_MINUTES)
            );

            await context.SaveChangesAsync(cancellationToken);

            // Send notification emails
            backgroundJobClient.Enqueue(
                () =>
                    SendExtensionSuccessEmail(
                        booking.User.Name,
                        booking.User.Email,
                        booking.Car.Model.Name,
                        booking.StartTime,
                        oldEndDate,
                        booking.StartTime,
                        newEndDate,
                        newTotalAmount
                    )
            );

            backgroundJobClient.Enqueue(
                () =>
                    SendExtensionSuccessEmail(
                        booking.Car.Owner.Name,
                        booking.Car.Owner.Email,
                        booking.Car.Model.Name,
                        booking.StartTime,
                        oldEndDate,
                        booking.StartTime,
                        newEndDate,
                        newTotalAmount
                    )
            );

            return Result<Response>.Success(
                new Response(
                    booking.Id,
                    booking.StartTime,
                    booking.EndTime,
                    newTotalAmount // Return only the additional amount
                ),
                "Gia hạn thời gian trả xe thành công"
            );
        }

        public async Task RevertExtensionIfNotPaid(
            Guid bookingId,
            DateTimeOffset oldEndDate,
            decimal oldBasePrice,
            decimal oldPlatformFee,
            decimal oldTotalAmount
        )
        {
            var booking = await context
                .Bookings.Include(b => b.User)
                .Include(b => b.Car)
                .ThenInclude(c => c.Owner)
                .Include(b => b.Car.Model)
                .Include(b => b.Contract)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.IsPaid)
                return;

            var requestedEndDate = booking.EndTime;

            // Revert both booking and contract
            booking.EndTime = oldEndDate;
            booking.BasePrice = oldBasePrice;
            booking.PlatformFee = oldPlatformFee;
            booking.TotalAmount = oldTotalAmount;
            booking.UpdatedAt = DateTimeOffset.UtcNow;

            // Revert contract dates
            booking.Contract.EndDate = oldEndDate;
            booking.Contract.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(CancellationToken.None);

            // Send notification emails
            await SendExtensionExpiredEmail(
                booking.User.Name,
                booking.User.Email,
                booking.Car.Model.Name,
                oldEndDate,
                requestedEndDate
            );

            await SendExtensionExpiredEmail(
                booking.Car.Owner.Name,
                booking.Car.Owner.Email,
                booking.Car.Model.Name,
                oldEndDate,
                requestedEndDate
            );
        }

        private async Task SendExtensionExpiredEmail(
            string name,
            string email,
            string carName,
            DateTimeOffset oldEndDate,
            DateTimeOffset requestedEndDate
        )
        {
            var template = ExtendBookingExpiredTemplate.Template(
                name,
                carName,
                oldEndDate,
                requestedEndDate
            );

            await emailService.SendEmailAsync(email, "Thông Báo Hết Hạn Gia Hạn Thuê Xe", template);
        }

        public async Task SendExtensionSuccessEmail(
            string name,
            string email,
            string carName,
            DateTimeOffset oldStartDate,
            DateTimeOffset oldEndDate,
            DateTimeOffset newStartDate,
            DateTimeOffset newEndDate,
            decimal? additionalAmount = null
        )
        {
            var template = ExtendBookingSuccessTemplate.Template(
                name,
                carName,
                oldStartDate,
                oldEndDate,
                newStartDate,
                newEndDate,
                additionalAmount
            );

            var subject = additionalAmount.HasValue
                ? "Thông Báo Gia Hạn Thuê Xe Thành Công"
                : "Thông Báo Thay Đổi Thời Gian Thuê Xe";

            await emailService.SendEmailAsync(email, subject, template);
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        private const int MAX_BOOKING_DAYS = 30;

        public Validator()
        {
            RuleFor(x => x.BookingId).NotEmpty().WithMessage("Booking ID không được để trống");

            RuleFor(x => x.NewStartTime)
                .NotEmpty()
                .WithMessage("Thời gian bắt đầu không được để trống")
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Thời gian bắt đầu phải sau thời gian hiện tại");

            RuleFor(x => x.NewEndTime)
                .NotEmpty()
                .WithMessage("Thời gian kết thúc không được để trống")
                .GreaterThan(x => x.NewStartTime)
                .WithMessage("Thời gian kết thúc phải sau thời gian bắt đầu");

            RuleFor(x => x)
                .Must(x => (x.NewEndTime - x.NewStartTime).TotalDays <= MAX_BOOKING_DAYS)
                .WithMessage($"Thời gian thuê phải từ 1 đến {MAX_BOOKING_DAYS} ngày");
        }
    }
}
