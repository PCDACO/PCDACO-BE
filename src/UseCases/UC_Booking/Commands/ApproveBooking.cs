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
using UseCases.BackgroundServices.Bookings;
using UseCases.DTOs;
using UseCases.Services.EmailService;
using UseCases.Services.PaymentTokenService;

namespace UseCases.UC_Booking.Commands;

public sealed class ApproveBooking
{
    private const int PAYMENT_EXPIRATION_HOURS = 12;

    public sealed record Command(Guid BookingId, bool IsApproved, string BaseUrl)
        : IRequest<Result>;

    internal sealed class Handler(
        IAppDBContext context,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        CurrentUser currentUser,
        IPaymentTokenService paymentTokenService
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsOwner())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context
                .Bookings.Include(x => x.Car)
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
            string? paymentToken = null;
            if (request.IsApproved && !booking.IsPaid)
            {
                paymentToken = await paymentTokenService.GenerateTokenAsync(booking.Id);
            }

            if (request.IsApproved)
            {
                await RejectOverlappingPendingBookingsAsync(booking, cancellationToken);

                // Update contract with Owner's signature.
                await UpdateContractForApprovalAsync(booking, cancellationToken);

                // Schedule expiration job if booking is not paid
                if (!booking.IsPaid)
                {
                    backgroundJobClient.Schedule<BookingExpiredJob>(
                        job => job.ExpireUnpaidApprovedBooking(booking.Id),
                        TimeSpan.FromHours(PAYMENT_EXPIRATION_HOURS)
                    );
                }
            }
            else
            {
                booking.Status = BookingStatusEnum.Rejected;
                booking.Note = "Chủ xe từ chối yêu cầu đặt xe";
            }

            booking.UpdatedAt = DateTimeOffset.UtcNow;
            booking.Status = request.IsApproved
                ? BookingStatusEnum.Approved
                : BookingStatusEnum.Rejected;

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
                        booking.TotalAmount,
                        paymentToken,
                        request.BaseUrl
                    )
            );

            string actionVerb = request.IsApproved ? "phê duyệt" : "từ chối";
            return Result.SuccessWithMessage($"Đã {actionVerb} booking thành công");
        }

        // Rejects overlapping bookings (if any) for the same car.
        private async Task RejectOverlappingPendingBookingsAsync(
            Booking booking,
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
                            overlappingBooking.TotalAmount,
                            null,
                            ""
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
            decimal totalAmount,
            string? paymentToken,
            string baseUrl
        )
        {
            var emailTemplate = DriverApproveBookingTemplate.Template(
                driverName,
                carModelName,
                startTime,
                endTime,
                totalAmount,
                isApproved,
                paymentToken,
                baseUrl
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

            RuleFor(x => x.BaseUrl).NotEmpty().WithMessage("Base URL không được để trống");
        }
    }
}
