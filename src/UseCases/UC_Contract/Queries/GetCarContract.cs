using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;
using static Domain.Shared.ContractTemplates.CarContractTemplateGenerator;

namespace UseCases.UC_Contract.Queries;

public sealed class GetCarContract
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(string HtmlContent);

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
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
                .Include(c => c.Technician)
                .ThenInclude(t => t.EncryptionKey)
                .FirstOrDefaultAsync(c => c.CarId == request.Id, cancellationToken);

            if (contract == null)
            {
                return Result.NotFound("Không tìm thấy hợp đồng.");
            }

            if (currentUser.User!.IsOwner() && contract.TechnicianSignatureDate == null)
            {
                return Result.Error("Vui lòng chờ kiểm định viên ký và xác nhận hợp đồng trước.");
            }

            var contractDate = GetTimestampFromUuid.Execute(contract.Id);

            string decryptedOwnerLicenseNumber = await DecryptOwnerLicenseNumber(contract);

            var contractTemplate = new CarContractTemplate
            {
                ContractNumber = contract.Id.ToString(),
                ContractDate = contractDate,
                OwnerName = contract.Car.Owner.Name,
                OwnerLicenseNumber = decryptedOwnerLicenseNumber,
                OwnerAddress = contract.Car.Owner.Address,
                TechnicianName = contract.Technician?.Name ?? string.Empty,
                CarManufacturer = contract.Car.Model.Name,
                CarLicensePlate = contract.Car.LicensePlate,
                CarSeat = contract.Car.Seat.ToString(),
                CarColor = contract.Car.Color,
                CarDescription = contract.Car.Description,
                CarPrice = contract.Car.Price,
                CarTerms = contract.Car.Terms,
                InspectionResults = contract.InspectionResults ?? string.Empty,
                InspectionPhotos = [],
                GPSDeviceId = contract.GPSDeviceId?.ToString() ?? string.Empty,
            };

            string html = GenerateFullContractHtml(contractTemplate);

            return Result.Success(new Response(html));
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
    }
}
