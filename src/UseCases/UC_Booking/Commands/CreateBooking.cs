using Ardalis.Result;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UUIDNext;

namespace UseCases.UC_Booking.Commands;

public sealed class CreateBooking
{
    public sealed record CreateBookingCommand(
        Guid UserId,
        Guid CarId,
        // Guid StatusId,
        DateTime StartTime,
        DateTime EndTime
    ) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(Booking booking) => new(booking.Id);
    }

    internal sealed class Handler(IAppDBContext appDBContext, CurrentUser currentUser)
        : IRequestHandler<CreateBookingCommand, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            CreateBookingCommand request,
            CancellationToken cancellationToken
        )
        {
            // Add Owner role later
            if (!currentUser.User!.IsDriver())
                return Result.Error("Bạn không có quyền thực hiện chức năng này !");

            var car = await appDBContext.Cars.FirstOrDefaultAsync(
                x => x.Id == request.CarId,
                cancellationToken: cancellationToken
            );

            if (car == null)
                return Result<Response>.NotFound();

            // Check for overlapping bookings (same user + same car)
            bool hasOverlap = await appDBContext.Bookings.AnyAsync(
                b =>
                    b.UserId == request.UserId
                    && b.CarId == request.CarId
                    && b.StartTime < request.EndTime
                    && b.EndTime > request.StartTime,
                cancellationToken
            );

            if (hasOverlap)
            {
                return Result.Conflict(
                    "Bạn đã có đơn đặt xe cho chiếc xe này trong khoảng thời gian này."
                );
            }

            Guid bookingId = Uuid.NewDatabaseFriendly(Database.PostgreSql);
            var totalBookingDay = (request.EndTime - request.StartTime).Days;
            var basePrice = car.PricePerDay * totalBookingDay;
            var platformFee = basePrice * 0.1m;
            var totalAmount = basePrice + platformFee;

            var booking = new Booking
            {
                Id = bookingId,
                UserId = request.UserId,
                CarId = request.CarId,
                // StatusId = request.StatusId,
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

            appDBContext.Bookings.Add(booking);
            await appDBContext.SaveChangesAsync(cancellationToken);

            return Result<Response>.Success(new Response(bookingId));
        }
    }

    public sealed class Validator : AbstractValidator<CreateBookingCommand>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("User không được để trống");

            RuleFor(x => x.CarId).NotEmpty().WithMessage("Car không được để trống");

            RuleFor(x => x.StartTime).NotEmpty().WithMessage("Phải chọn thời gian bắt đầu thuê");

            RuleFor(x => x.EndTime).NotEmpty().WithMessage("Phải chọn thời gian kết thúc thuê");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage("Thời gian kết thúc thuê phải sau thời gian bắt đầu thuê");
        }
    }
}
