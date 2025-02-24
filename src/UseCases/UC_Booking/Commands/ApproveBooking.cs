using Ardalis.Result;
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

public sealed class ApproveBooking
{
    public sealed record Command(Guid BookingId, bool IsApproved) : IRequest<Result>;

    internal sealed class Handler(
        IAppDBContext context,
        IEmailService emailService,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsOwner())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context
                .Bookings.Include(x => x.Status)
                .Include(x => x.Car)
                .ThenInclude(x => x.Model)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.Car.OwnerId != currentUser.User.Id)
                return Result.Forbidden("Bạn không có quyền phê duyệt booking cho xe này!");

            // Validate current status
            var invalidStatuses = new[]
            {
                BookingStatusEnum.Approved,
                BookingStatusEnum.Rejected,
                BookingStatusEnum.Ongoing,
                BookingStatusEnum.Completed,
                BookingStatusEnum.Cancelled
            };

            if (invalidStatuses.Contains(booking.Status.Name.ToEnum()))
            {
                return Result.Conflict(
                    $"Không thể phê duyệt booking ở trạng thái {booking.Status.Name}"
                );
            }

            string statusName = request.IsApproved
                ? BookingStatusEnum.Approved.ToString()
                : BookingStatusEnum.Rejected.ToString();
            string message = request.IsApproved ? "phê duyệt" : "từ chối";

            var status = await context
                .BookingStatuses.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => EF.Functions.ILike(x.Name, statusName),
                    cancellationToken
                );

            if (status == null)
                return Result.NotFound("Không tìm thấy trạng thái phù hợp");

            // TODO: set car status to rented
            // TODO: create background job to update car status to available if driver doesn't start booking after 1 day after start-trip time

            booking.StatusId = status.Id;
            await context.SaveChangesAsync(cancellationToken);

            BackgroundJob.Enqueue(
                () =>
                    SendEmail(
                        request.IsApproved,
                        booking.User.Name,
                        booking.User.Email,
                        booking.Car.Model.Name,
                        booking.StartTime,
                        booking.EndTime,
                        booking.TotalAmount
                    )
            );

            return Result.SuccessWithMessage($"Đã {message} booking thành công");
        }

        public async Task SendEmail(
            bool isApproved,
            string driverName,
            string driverEmail,
            string carModelName,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            decimal totalAmount
        )
        {
            // Send email to driver
            var emailTemplate = DriverApproveBookingTemplate.Template(
                driverName,
                carModelName,
                startTime,
                endTime,
                totalAmount,
                isApproved
            );

            await emailService.SendEmailAsync(driverEmail, "Phê duyệt đặt xe", emailTemplate);
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BookingId).NotEmpty().WithMessage("Booking id không được để trống");

            RuleFor(x => x.IsApproved)
                .NotNull()
                .WithMessage("Trạng thái phê duyệt không được để trống");
        }
    }
}
