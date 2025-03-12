using Ardalis.Result;
using Domain.Constants.EntityNames;
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

public sealed class ApproveBooking
{
    public sealed record Command(Guid BookingId, bool IsApproved) : IRequest<Result>;

    internal sealed class Handler(
        IAppDBContext context,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsOwner())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context
                .Bookings
                .Include(x => x.Car)
                .ThenInclude(x => x.Model)
                .Include(x => x.User)
                .Include(x => x.Contract)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.Car.OwnerId != currentUser.User.Id)
                return Result.Forbidden("Bạn không có quyền phê duyệt booking cho xe này!");

            // Check if the booking is in a modifiable status.
            if (booking.Status != BookingStatusEnum.Pending)
            {
                return Result.Conflict(
                    $"Không thể phê duyệt booking ở trạng thái " + booking.Status.ToString()
                );
            }
            // Process approval (update booking, possibly update contract)
            if (request.IsApproved)
            {
                await RejectOverlappingPendingBookingsAsync(
                    booking,
                    BookingStatusEnum.Rejected,
                    cancellationToken
                );

                // Mark the car as rented.
                booking.Car.Status = CarStatusEnum.Rented;

                // Update contract with Owner's signature.
                await UpdateContractForApprovalAsync(booking, cancellationToken);
            }
            else
            {
                // If booking is rejected, provide a full refund
                booking.Status = BookingStatusEnum.Rejected;
                booking.Note = "Chủ xe từ chối yêu cầu đặt xe";

                if (booking.IsPaid)
                {
                    booking.IsRefund = true;
                    booking.RefundAmount = booking.TotalAmount;

                    var admin = await context.Users.FirstOrDefaultAsync(u =>
                        u.Role.Name == UserRoleNames.Admin
                    );

                    if (admin != null)
                    {
                        var adminAmount = booking.RefundAmount * 0.1m;
                        var ownerAmount = booking.RefundAmount * 0.9m;

                        admin.Balance -= (decimal)adminAmount;
                        booking.Car.Owner.Balance -= (decimal)ownerAmount;
                        booking.User.Balance += (decimal)booking.RefundAmount;
                    }
                }
            }

            booking.Status = request.IsApproved ? BookingStatusEnum.Approved : BookingStatusEnum.Rejected;
            await context.SaveChangesAsync(cancellationToken);

            // Enqueue email notification
            backgroundJobClient.Enqueue(
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

            string actionVerb = request.IsApproved ? "phê duyệt" : "từ chối";
            return Result.SuccessWithMessage($"Đã {actionVerb} booking thành công");
        }

        // Rejects overlapping bookings (if any) for the same car.
        private async Task RejectOverlappingPendingBookingsAsync(
            Booking booking,
            BookingStatusEnum rejectedStatus,
            CancellationToken cancellationToken
        )
        {
            // First get the overlapping bookings to access their TotalAmount
            var overlappingBookings = await context
                .Bookings.Where(b =>
                    b.CarId == booking.CarId
                    && b.StartTime < booking.EndTime
                    && b.EndTime > booking.StartTime
                    && b.Status == BookingStatusEnum.Pending
                    && b.Id != booking.Id
                )
                .ToListAsync(cancellationToken);

            foreach (var overlappingBooking in overlappingBookings)
            {
                overlappingBooking.Status = BookingStatusEnum.Rejected;
                overlappingBooking.Note = "Đã có booking khác trùng lịch";

                if (overlappingBooking.IsPaid)
                {
                    overlappingBooking.IsRefund = true;
                    overlappingBooking.RefundAmount = overlappingBooking.TotalAmount; // Full refund
                }
            }

            if (!overlappingBookings.Any())
                return;

            await context.SaveChangesAsync(cancellationToken);

            foreach (var overlappingBooking in overlappingBookings)
            {
                // Enqueue email notification
                backgroundJobClient.Enqueue(
                    () =>
                        SendEmail(
                            false, // IsApproved
                            overlappingBooking.User.Name,
                            overlappingBooking.User.Email,
                            overlappingBooking.Car.Model.Name,
                            overlappingBooking.StartTime,
                            overlappingBooking.EndTime,
                            overlappingBooking.TotalAmount
                        )
                );
            }
        }

        // If the contract exists, update it with the owner's signature and mark as confirmed.
        private async Task UpdateContractForApprovalAsync(
            Booking booking,
            CancellationToken cancellationToken
        )
        {
            var contract =
                booking.Contract
                ?? await context.Contracts.FirstOrDefaultAsync(
                    c => c.BookingId == booking.Id,
                    cancellationToken
                );

            if (contract != null)
            {
                contract.OwnerSignatureDate = DateTimeOffset.UtcNow;
                contract.Status = ContractStatusEnum.Confirmed;
            }
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