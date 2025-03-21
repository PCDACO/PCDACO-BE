using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using Domain.Shared.ContractTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Services.PdfService;
using UseCases.Utils;
using static Domain.Shared.ContractTemplates.CarContractTemplateGenerator;

namespace UseCases.UC_Contract.Queries;

public sealed class GetCarContract
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(byte[] PdfFile, string FileName);

    internal sealed class Handler(
        IAppDBContext context,
        IPdfService pdfService,
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
                .CarContracts.AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.Car)
                .ThenInclude(car => car.Model)
                .Include(c => c.Car)
                .ThenInclude(car => car.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(c => c.Car)
                .ThenInclude(car => car.EncryptionKey)
                .Include(c => c.Technician)
                .ThenInclude(t => t.EncryptionKey)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (contract == null)
            {
                return Result.NotFound("Không tìm thấy hợp đồng.");
            }

            var contractDate = GetTimestampFromUuid.Execute(contract.Id);

            string decryptedOwnerLicenseNumber = await DecryptOwnerLicenseNumber(contract);
            string decryptedTechnicianLicenseNumber = await DecryptTechnicianLicenseNumber(
                contract
            );
            string decryptedCarLicensePlate = await DecryptCarLicensePlate(contract);

            var contractTemplate = new CarContractTemplate
            {
                ContractNumber = contract.Id.ToString(),
                ContractDate = contractDate,
                OwnerName = contract.Car.Owner.Name,
                OwnerLicenseNumber = decryptedOwnerLicenseNumber,
                OwnerAddress = contract.Car.Owner.Address,
                TechnicianName = contract.Technician?.Name,
                TechnicianLicenseNumber = decryptedTechnicianLicenseNumber,
                CarManufacturer = contract.Car.Model.Name,
                CarLicensePlate = decryptedCarLicensePlate,
                CarSeat = contract.Car.Seat.ToString(),
                CarColor = contract.Car.Color,
                CarDescription = contract.Car.Description,
                CarPrice = contract.Car.Price,
                CarTerms = contract.Car.Terms,
                InspectionResults = contract.InspectionResults,
                InspectionPhotos = [],
                // contract.Photos.ToDictionary(p => p.Type, p => p.PhotoUrl),
                GPSDeviceId = contract.GPSDeviceId?.ToString()
            };

            string html = GenerateFullContractHtml(contractTemplate);

            var pdfFile = pdfService.ConvertHtmlToPdf(html);

            var contractDateUnixTime = contractDate.ToUnixTimeSeconds();

            string fileName = $"HopDongKiemDinh_{contractDateUnixTime}.pdf";

            return Result.Success(new Response(pdfFile, fileName));
        }

        private async Task<string> DecryptOwnerLicenseNumber(CarContract contract)
        {
            var ownerKey = keyManagementService.DecryptKey(
                contract.Car.Owner.EncryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            var decryptedOwnerLicenseNumber = await aesEncryptionService.Decrypt(
                contract.Car.Owner.EncryptedLicenseNumber,
                ownerKey,
                contract.Car.Owner.EncryptionKey.IV
            );

            return decryptedOwnerLicenseNumber;
        }

        private async Task<string> DecryptTechnicianLicenseNumber(CarContract contract)
        {
            if (contract.Technician == null || contract.Technician.EncryptionKey == null)
                return string.Empty;

            var technicianKey = keyManagementService.DecryptKey(
                contract.Technician.EncryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            var decryptedTechnicianLicenseNumber = await aesEncryptionService.Decrypt(
                contract.Technician.EncryptedLicenseNumber,
                technicianKey,
                contract.Technician.EncryptionKey.IV
            );

            return decryptedTechnicianLicenseNumber;
        }

        private async Task<string> DecryptCarLicensePlate(CarContract contract)
        {
            var carKey = keyManagementService.DecryptKey(
                contract.Car.EncryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            var decryptedCarLicensePlate = await aesEncryptionService.Decrypt(
                contract.Car.EncryptedLicensePlate,
                carKey,
                contract.Car.EncryptionKey.IV
            );

            return decryptedCarLicensePlate;
        }
    }
}
