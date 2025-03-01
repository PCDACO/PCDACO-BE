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
                .Bookings.Include(x => x.Status)
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
            if (IsBookingInInvalidStatus(booking))
            {
                return Result.Conflict(
                    $"Không thể phê duyệt booking ở trạng thái {booking.Status.Name}"
                );
            }

            // Retrieve approved and rejected statuses.
            var (approvedStatus, rejectedStatus) = await GetApprovalStatuses(cancellationToken);

            if (approvedStatus == null || rejectedStatus == null)
                return Result.NotFound("Không tìm thấy trạng thái phù hợp");

            // Retrieve car status (for the rental state).
            var rentedCarStatus = await GetRentedCarStatusAsync(cancellationToken);

            if (rentedCarStatus == null)
                return Result.NotFound("Không tìm thấy trạng thái xe phù hợp");

            // Process approval (update booking, possibly update contract)
            if (request.IsApproved)
            {
                await RejectOverlappingPendingBookingsAsync(
                    booking,
                    rejectedStatus,
                    cancellationToken
                );

                // Mark the car as rented.
                booking.Car.StatusId = rentedCarStatus.Id;

                // Update contract with Owner's signature.
                await UpdateContractForApprovalAsync(booking, cancellationToken);
            }

            booking.StatusId = request.IsApproved ? approvedStatus.Id : rejectedStatus.Id;
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

        private static bool IsBookingInInvalidStatus(Booking booking)
        {
            BookingStatusEnum[] invalidStatuses =
            [
                BookingStatusEnum.Approved,
                BookingStatusEnum.Rejected,
                BookingStatusEnum.Ongoing,
                BookingStatusEnum.Completed,
                BookingStatusEnum.Cancelled,
                BookingStatusEnum.Expired
            ];

            return invalidStatuses.Contains(booking.Status.Name.ToEnum());
        }

        // Retrieves both the approved and rejected statuses.
        private async Task<(BookingStatus? Approved, BookingStatus? Rejected)> GetApprovalStatuses(
            CancellationToken cancellationToken
        )
        {
            var statuses = await context
                .BookingStatuses.AsNoTracking()
                .Where(x =>
                    EF.Functions.ILike(x.Name, BookingStatusEnum.Approved.ToString())
                    || EF.Functions.ILike(x.Name, BookingStatusEnum.Rejected.ToString())
                )
                .ToListAsync(cancellationToken);

            var approvedStatus = statuses.FirstOrDefault(x =>
                x.Name == BookingStatusEnum.Approved.ToString()
            );
            var rejectedStatus = statuses.FirstOrDefault(x =>
                x.Name == BookingStatusEnum.Rejected.ToString()
            );
            return (approvedStatus, rejectedStatus);
        }

        // Retrieves the car status for “Rented”.
        private async Task<CarStatus?> GetRentedCarStatusAsync(CancellationToken cancellationToken)
        {
            return await context
                .CarStatuses.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => EF.Functions.ILike(x.Name, CarStatusNames.Rented),
                    cancellationToken
                );
        }

        // Rejects overlapping bookings (if any) for the same car.
        private async Task RejectOverlappingPendingBookingsAsync(
            Booking booking,
            BookingStatus rejectedStatus,
            CancellationToken cancellationToken
        )
        {
            await context
                .Bookings.Where(b =>
                    b.CarId == booking.CarId
                    && b.StartTime < booking.EndTime
                    && b.EndTime > booking.StartTime
                    && b.Status.Name == BookingStatusEnum.Pending.ToString()
                    && b.Id != booking.Id
                )
                .ExecuteUpdateAsync(
                    b =>
                        b.SetProperty(b => b.StatusId, rejectedStatus.Id)
                            .SetProperty(b => b.Note, "Đã có booking khác trùng lịch"),
                    cancellationToken: cancellationToken
                );
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

                var confirmedStatus = await context.ContractStatuses.FirstOrDefaultAsync(
                    cs => cs.Name == ContractStatusNames.Confirmed,
                    cancellationToken: cancellationToken
                );

                if (confirmedStatus != null)
                {
                    contract.StatusId = confirmedStatus.Id;
                }
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
