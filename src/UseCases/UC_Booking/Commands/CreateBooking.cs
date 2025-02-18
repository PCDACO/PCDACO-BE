using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
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

            // Check if car exists
            var car = await context
                .Cars.Include(x => x.CarStatistic)
                .Include(x => x.Owner)
                .Include(x => x.Model)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == request.CarId
                        && EF.Functions.ILike(x.CarStatus.Name, $"%available%"),
                    cancellationToken: cancellationToken
                );

            if (car == null)
                return Result<Response>.NotFound();

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
            var basePrice = car.PricePerDay * (decimal)totalBookingDay;
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

            await SendEmail(request, car, totalAmount, booking);

            return Result<Response>.Success(new Response(bookingId));
        }

        private async Task SendEmail(
            CreateBookingCommand request,
            Car car,
            decimal totalAmount,
            Booking booking
        )
        {
            // Send email to driver
            var emailTemplate = DriverCreateBookingTemplate.Template(
                currentUser.User.Name,
                car.Model.Name,
                request.StartTime,
                request.EndTime,
                totalAmount
            );

            await emailService.SendEmailAsync(
                currentUser.User.Email,
                "Xác nhận đặt xe",
                emailTemplate
            );

            // Send email to owner
            var emailTemplateOwner = OwnerCreateBookingTemplate.Template(
                car.Owner.Name,
                car.Model.Name,
                booking.User.Name,
                request.StartTime,
                request.EndTime,
                totalAmount
            );

            await emailService.SendEmailAsync(
                car.Owner.Email,
                "Yêu Cầu Đặt Xe Mới",
                emailTemplateOwner
            );
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
