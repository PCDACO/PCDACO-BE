using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using Domain.Shared.ContractTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_Contract.Queries;

public sealed class GetBookingContract
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(string HtmlContent);

    internal sealed class Handler(
        IAppDBContext context,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var contract = await context
                .Contracts.AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.Booking)
                .ThenInclude(b => b.User)
                .Include(c => c.Booking)
                .ThenInclude(b => b.User)
                .ThenInclude(u => u.EncryptionKey)
                .Include(c => c.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(car => car.Model)
                .Include(c => c.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(car => car.Owner)
                .ThenInclude(l => l.EncryptionKey)
                .FirstOrDefaultAsync(c => c.BookingId == request.Id, cancellationToken);

            if (contract == null)
            {
                return Result.NotFound("Không tìm thấy hợp đồng.");
            }

            var contractDate = GetTimestampFromUuid.Execute(contract.Id);

            string decryptedDriverLicenseNumber = await DecryptDriverLicenseNumber(contract);
            string decryptedOwnerLicenseNumber = await DecryptOwnerLicenseNumber(contract);

            var contractTemplate = new ContractTemplateGenerator.ContractTemplate
            {
                ContractNumber = contract.Id.ToString(),
                ContractDate = contractDate,
                OwnerName = contract.Booking.Car.Owner.Name,
                OwnerLicenseNumber = decryptedOwnerLicenseNumber,
                OwnerAddress = contract.Booking.Car.Owner.Address,
                DriverName = contract.Booking.User.Name,
                DriverLicenseNumber = decryptedDriverLicenseNumber,
                DriverAddress = contract.Booking.User.Address,
                CarManufacturer = contract.Booking.Car.Model.Name,
                CarLicensePlate = contract.Booking.Car.LicensePlate,
                CarSeat = contract.Booking.Car.Seat.ToString(),
                CarColor = contract.Booking.Car.Color,
                CarDetail = contract.Booking.Car.Description,
                CarTerms = contract.Booking.Car.Terms,
                RentalPrice = contract.Booking.BasePrice.ToString("N0"),
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                PickupAddress = contract.Booking.Car.PickupAddress,
                OwnerSignatureImageUrl = contract.OwnerSignature ?? string.Empty,
                DriverSignatureImageUrl = contract.DriverSignature ?? string.Empty,
            };

            string html = ContractTemplateGenerator.GenerateFullContractHtml(contractTemplate);

            return Result.Success(new Response(html));
        }

        private async Task<string> DecryptDriverLicenseNumber(Contract contract)
        {
            var driverKey = keyManagementService.DecryptKey(
                contract.Booking.User.EncryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            var decryptedDriverLicenseNumber = await aesEncryptionService.Decrypt(
                contract.Booking.User.EncryptedLicenseNumber,
                driverKey,
                contract.Booking.User.EncryptionKey.IV
            );

            return decryptedDriverLicenseNumber;
        }

        private async Task<string> DecryptOwnerLicenseNumber(Contract contract)
        {
            var ownerKey = keyManagementService.DecryptKey(
                contract.Booking.Car.Owner.EncryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            var decryptedOwnerLicenseNumber = await aesEncryptionService.Decrypt(
                contract.Booking.Car.Owner.EncryptedLicenseNumber,
                ownerKey,
                contract.Booking.Car.Owner.EncryptionKey.IV
            );

            return decryptedOwnerLicenseNumber;
        }
    }
}
