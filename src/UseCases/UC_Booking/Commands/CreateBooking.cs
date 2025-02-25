using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.BackgroundServices.Bookings;
using UseCases.DTOs;
using UseCases.Services.EmailService;
using UUIDNext;

namespace UseCases.UC_Booking.Commands;

public sealed class CreateBooking
{
    public sealed record CreateBookingCommand(Guid CarId, DateTime StartTime, DateTime EndTime)
        : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(Booking booking) => new(booking.Id);
    }

    internal sealed class Handler(
        IAppDBContext context,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        CurrentUser currentUser
    ) : IRequestHandler<CreateBookingCommand, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            CreateBookingCommand request,
            CancellationToken cancellationToken
        )
        {
            // Add Owner role later
            if (!currentUser.User!.IsDriver())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            // Verify driver license first
            var license = await context.Licenses.FirstOrDefaultAsync(
                x => x.UserId == currentUser.User.Id,
                cancellationToken
            );

            if (license == null || !license.IsApprove.HasValue || !license.IsApprove.Value)
                return Result.Forbidden(
                    "Bạn chưa xác thực bằng lái xe hoặc bằng lái xe chưa được phê duyệt!"
                );

            // Check if car exists
            var car = await context
                .Cars.AsSplitQuery()
                .AsNoTracking()
                .Include(x => x.CarStatistic)
                .Include(x => x.Owner)
                .Include(x => x.Model)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == request.CarId
                        && EF.Functions.ILike(x.CarStatus.Name, $"%available%"),
                    cancellationToken: cancellationToken
                );

            if (car == null)
                return Result<Response>.NotFound("Không tìm thấy xe phù hợp");

            var bookingStatus = await context
                .BookingStatuses.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => EF.Functions.ILike(x.Name, BookingStatusEnum.Pending.ToString()),
                    cancellationToken: cancellationToken
                );

            if (bookingStatus == null)
                return Result<Response>.NotFound("Không tìm thấy trạng thái phù hợp");

            // Check for overlapping bookings (same user + same car)
            bool hasOverlap = await context
                .Bookings.AsNoTracking()
                .AnyAsync(
                    b =>
                        b.UserId == currentUser.User.Id
                        && b.CarId == request.CarId
                        && b.StartTime < request.EndTime
                        && b.EndTime > request.StartTime
                        && b.Status.Name != BookingStatusEnum.Rejected.ToString() // Exclude rejected bookings
                        && b.Status.Name != BookingStatusEnum.Cancelled.ToString(), // Exclude cancelled bookings
                    cancellationToken
                );

            if (hasOverlap)
            {
                return Result.Conflict(
                    "Bạn đã có đơn đặt xe cho chiếc xe này trong khoảng thời gian này."
                );
            }

            var userStatistic = await context.UserStatistics.FirstOrDefaultAsync(
                x => x.UserId == currentUser.User.Id,
                cancellationToken
            );

            if (userStatistic == null)
                return Result.NotFound("Không tìm thấy thông tin thống kê của user");
            const decimal platformFeeRate = 0.1m;

            Guid bookingId = Uuid.NewDatabaseFriendly(Database.PostgreSql);
            var totalBookingDay = Math.Ceiling((request.EndTime - request.StartTime).TotalDays);
            var basePrice = car.Price * (decimal)totalBookingDay;
            var platformFee = basePrice * platformFeeRate;
            var totalAmount = basePrice + platformFee;

            var booking = new Booking
            {
                Id = bookingId,
                UserId = currentUser.User.Id,
                CarId = request.CarId,
                StatusId = bookingStatus.Id,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                ActualReturnTime = request.EndTime, // Update later when user return car
                BasePrice = basePrice,
                PlatformFee = platformFee,
                ExcessDay = 0,
                ExcessDayFee = 0,
                TotalAmount = totalAmount,
                Note = string.Empty,
            };

            // Update car statistic
            car.CarStatistic.TotalRented += 1;
            userStatistic.TotalBooking += 1;

            context.Bookings.Add(booking);
            await context.SaveChangesAsync(cancellationToken);

            backgroundJobClient.Enqueue(
                () =>
                    SendEmail(
                        request.StartTime,
                        request.EndTime,
                        totalAmount,
                        currentUser.User.Name,
                        currentUser.User.Email,
                        car.Owner.Name,
                        car.Owner.Email,
                        car.Model.Name
                    )
            );

            backgroundJobClient.Enqueue<BookingExpiredJob>(job => job.ExpireOldBookings());

            backgroundJobClient.Schedule(() => NotifyOwner(booking.Id), TimeSpan.FromDays(1));

            return Result<Response>.Success(new Response(bookingId));
        }

        public async Task SendEmail(
            DateTime startTime,
            DateTime endTime,
            decimal totalAmount,
            string driverName,
            string driverEmail,
            string ownerName,
            string ownerEmail,
            string carModelName
        )
        {
            var driverEmailTemplate = DriverCreateBookingTemplate.Template(
                driverName,
                carModelName,
                startTime,
                endTime,
                totalAmount
            );

            await emailService.SendEmailAsync(driverEmail, "Xác nhận đặt xe", driverEmailTemplate);

            var ownerEmailTemplate = OwnerCreateBookingTemplate.Template(
                ownerName,
                carModelName,
                driverName,
                startTime,
                endTime,
                totalAmount
            );

            await emailService.SendEmailAsync(ownerEmail, "Yêu Cầu Đặt Xe Mới", ownerEmailTemplate);
        }

        public async Task NotifyOwner(Guid bookingId)
        {
            var booking = await context
                .Bookings.Include(b => b.User)
                .Include(b => b.Car)
                .ThenInclude(c => c.Owner)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking != null && booking.Status.Name == BookingStatusEnum.Pending.ToString())
            {
                // Send notification email to the owner
                var emailTemplate = OwnerNotificationTemplate.Template(
                    booking.Car.Owner.Name,
                    booking.User.Name,
                    booking.Car.Model.Name,
                    booking.StartTime,
                    booking.EndTime,
                    booking.TotalAmount
                );

                await emailService.SendEmailAsync(
                    booking.Car.Owner.Email,
                    "Booking Approval Reminder",
                    emailTemplate
                );
            }
        }
    }

    public sealed class Validator : AbstractValidator<CreateBookingCommand>
    {
        public Validator()
        {
            RuleFor(x => x.CarId).NotEmpty().WithMessage("Car không được để trống");

            RuleFor(x => x.StartTime)
                .NotEmpty()
                .WithMessage("Phải chọn thời gian bắt đầu thuê")
                .GreaterThan(DateTime.Now)
                .WithMessage("Thời gian bắt đầu thuê phải sau thời gian hiện tại");

            RuleFor(x => x.EndTime).NotEmpty().WithMessage("Phải chọn thời gian kết thúc thuê");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage("Thời gian kết thúc thuê phải sau thời gian bắt đầu thuê");
        }
    }
}
