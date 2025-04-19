using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Domain.Shared.ContractTemplates;
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
using static Domain.Shared.ContractTemplates.ContractTemplateGenerator;

namespace UseCases.UC_Booking.Commands;

public sealed class ApproveBooking
{
    private const int PAYMENT_EXPIRATION_HOURS = 12;

    public sealed record Command(Guid BookingId, bool IsApproved, string BaseUrl, string Signature)
        : IRequest<Result>;

    internal sealed class Handler(
        IAppDBContext context,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        CurrentUser currentUser,
        IPaymentTokenService paymentTokenService,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
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
                .ThenInclude(x => x.EncryptionKey)
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
                await UpdateContractForApprovalAsync(booking, request.Signature, cancellationToken);

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
            string signature,
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
                // Get car and owner details
                var car = await context
                    .Cars.Include(c => c.Owner)
                    .ThenInclude(c => c.EncryptionKey)
                    .Include(c => c.Model)
                    .Include(c => c.GPS)
                    .FirstOrDefaultAsync(c => c.Id == booking.CarId, cancellationToken);

                if (car == null)
                    return;

                // Create contract template
                var contractTemplate = new ContractTemplate
                {
                    ContractNumber = contract.Id.ToString(),
                    ContractDate = DateTimeOffset.UtcNow,
                    OwnerName = car.Owner.Name,
                    OwnerLicenseNumber = await DecryptValue(
                        car.Owner.EncryptedLicenseNumber,
                        car.Owner.EncryptionKey,
                        aesEncryptionService
                    ),
                    OwnerAddress = car.Owner.Address,
                    DriverName = booking.User.Name,
                    DriverLicenseNumber = await DecryptValue(
                        booking.User.EncryptedLicenseNumber,
                        booking.User.EncryptionKey,
                        aesEncryptionService
                    ),
                    DriverAddress = booking.User.Address,
                    CarManufacturer = car.Model.Name,
                    CarLicensePlate = car.LicensePlate,
                    CarSeat = car.Seat.ToString(),
                    CarColor = car.Color,
                    CarDetail = car.Description,
                    CarTerms = car.Terms,
                    RentalPrice = car.Price.ToString(),
                    StartDate = booking.StartTime,
                    EndDate = booking.EndTime,
                    PickupAddress = car.GPS?.Location.ToString() ?? "Địa chỉ không xác định",
                    OwnerSignatureImageUrl = signature,
                    DriverSignatureImageUrl = contract.DriverSignature ?? string.Empty,
                };

                // Generate contract terms
                string contractHtml = GenerateFullContractHtml(contractTemplate);

                // Update contract
                contract.Terms = contractHtml;
                contract.OwnerSignatureDate = DateTimeOffset.UtcNow;
                contract.OwnerSignature = signature;
                contract.Status = ContractStatusEnum.Confirmed;
                contract.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        private async Task<string> DecryptValue(
            string encryptedValue,
            EncryptionKey encryptionKey,
            IAesEncryptionService aesEncryptionService
        )
        {
            var decryptedKey = keyManagementService.DecryptKey(
                encryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            return await aesEncryptionService.Decrypt(
                encryptedValue,
                decryptedKey,
                encryptionKey.IV
            );
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
            // when request.IsApproved = true then require signature
            When(
                x => x.IsApproved,
                () => RuleFor(x => x.Signature).NotEmpty().WithMessage("Chữ ký không được để trống"));
        }
    }
}
