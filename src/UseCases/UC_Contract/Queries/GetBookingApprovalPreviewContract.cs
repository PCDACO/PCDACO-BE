using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using Domain.Shared.ContractTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Contract.Queries;

public sealed class GetBookingApprovalPreviewContract
{
    public sealed record Query(Guid BookingId) : IRequest<Result<Response>>;

    public sealed record Response(string HtmlContent);

    internal sealed class Handler(
        IAppDBContext context,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings,
        ContractSettings contractSettings,
        CurrentUser currentUser
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Get booking and related data
            var booking = await context
                .Bookings.AsNoTracking()
                .AsSplitQuery()
                .Include(b => b.Car)
                .ThenInclude(c => c.Model)
                .Include(b => b.Car)
                .ThenInclude(c => c.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(b => b.User)
                .ThenInclude(u => u.EncryptionKey)
                .Include(b => b.Contract)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            // Verify owner access
            if (booking.Car.OwnerId != currentUser.User!.Id)
                return Result.Forbidden("Bạn không có quyền xem hợp đồng này");

            // Verify booking status
            if (booking.Status != BookingStatusEnum.Pending)
                return Result.Error($"Booking không ở trạng thái chờ phê duyệt");

            // Decrypt sensitive information
            string decryptedOwnerLicenseNumber = await DecryptOwnerLicenseNumber(booking);
            string decryptedDriverLicenseNumber = await DecryptDriverLicenseNumber(booking);

            var contractTemplate = new ContractTemplateGenerator.ContractTemplate
            {
                ContractNumber = $"(Dự thảo - {booking.Id})",
                ContractDate = DateTimeOffset.UtcNow,
                OwnerName = booking.Car.Owner.Name,
                OwnerLicenseNumber = decryptedOwnerLicenseNumber,
                OwnerAddress = booking.Car.Owner.Address,
                DriverName = booking.User.Name,
                DriverLicenseNumber = decryptedDriverLicenseNumber,
                DriverAddress = booking.User.Address,
                CarManufacturer = booking.Car.Model.Name,
                CarLicensePlate = booking.Car.LicensePlate,
                CarSeat = booking.Car.Seat.ToString(),
                CarColor = booking.Car.Color,
                CarDetail = booking.Car.Description,
                CarTerms = booking.Car.Terms,
                RentalPrice = booking.BasePrice.ToString("N0"),
                StartDate = booking.StartTime,
                EndDate = booking.EndTime,
                PickupAddress = booking.Car.PickupAddress,
                OwnerSignatureImageUrl = booking.Contract?.OwnerSignature ?? string.Empty,
                DriverSignatureImageUrl = booking.Contract?.DriverSignature ?? string.Empty,
            };

            string html = ContractTemplateGenerator.GenerateFullContractHtml(contractTemplate, contractSettings);

            return Result.Success(new Response(html));
        }

        private async Task<string> DecryptDriverLicenseNumber(Booking booking)
        {
            var driverKey = keyManagementService.DecryptKey(
                booking.User.EncryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            return await aesEncryptionService.Decrypt(
                booking.User.EncryptedLicenseNumber,
                driverKey,
                booking.User.EncryptionKey.IV
            );
        }

        private async Task<string> DecryptOwnerLicenseNumber(Booking booking)
        {
            var ownerKey = keyManagementService.DecryptKey(
                booking.Car.Owner.EncryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            return await aesEncryptionService.Decrypt(
                booking.Car.Owner.EncryptedLicenseNumber,
                ownerKey,
                booking.Car.Owner.EncryptionKey.IV
            );
        }
    }
}
