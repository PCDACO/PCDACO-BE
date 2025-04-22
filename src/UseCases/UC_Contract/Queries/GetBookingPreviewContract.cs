using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using Domain.Shared.ContractTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Contract.Queries;

public sealed class GetBookingPreviewContract
{
    public sealed record Query(Guid CarId, DateTimeOffset StartTime, DateTimeOffset EndTime)
        : IRequest<Result<Response>>;

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
            // Get car and related data
            var car = await context
                .Cars.AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.Model)
                .Include(c => c.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .FirstOrDefaultAsync(c => c.Id == request.CarId, cancellationToken);

            if (car == null)
                return Result.NotFound("Không tìm thấy xe");

            // Get current user (driver) data
            var driver = await context
                .Users.AsNoTracking()
                .Include(u => u.EncryptionKey)
                .FirstOrDefaultAsync(u => u.Id == currentUser.User!.Id, cancellationToken);

            if (driver == null)
                return Result.NotFound("Không tìm thấy thông tin người dùng");

            // Calculate rental details
            var totalBookingDay = Math.Ceiling((request.EndTime - request.StartTime).TotalDays);
            var basePrice = car.Price * (decimal)totalBookingDay;

            // Decrypt sensitive information
            string decryptedOwnerLicenseNumber = await DecryptOwnerLicenseNumber(car);
            string decryptedDriverLicenseNumber = await DecryptDriverLicenseNumber(driver);

            var contractTemplate = new ContractTemplateGenerator.ContractTemplate
            {
                ContractNumber = "(Dự thảo)",
                ContractDate = DateTimeOffset.UtcNow,
                OwnerName = car.Owner.Name,
                OwnerLicenseNumber = decryptedOwnerLicenseNumber,
                OwnerAddress = car.Owner.Address,
                DriverName = driver.Name,
                DriverLicenseNumber = decryptedDriverLicenseNumber,
                DriverAddress = driver.Address,
                CarManufacturer = car.Model.Name,
                CarLicensePlate = car.LicensePlate,
                CarSeat = car.Seat.ToString(),
                CarColor = car.Color,
                CarDetail = car.Description,
                CarTerms = car.Terms,
                RentalPrice = basePrice.ToString("N0"),
                StartDate = request.StartTime,
                EndDate = request.EndTime,
                PickupAddress = car.PickupAddress,
                OwnerSignatureImageUrl = string.Empty,
                DriverSignatureImageUrl = string.Empty,
            };

            string html = ContractTemplateGenerator.GenerateFullContractHtml(
                contractTemplate,
                contractSettings
            );

            return Result.Success(new Response(html));
        }

        private async Task<string> DecryptDriverLicenseNumber(User driver)
        {
            var driverKey = keyManagementService.DecryptKey(
                driver.EncryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            return await aesEncryptionService.Decrypt(
                driver.EncryptedLicenseNumber,
                driverKey,
                driver.EncryptionKey.IV
            );
        }

        private async Task<string> DecryptOwnerLicenseNumber(Car car)
        {
            var ownerKey = keyManagementService.DecryptKey(
                car.Owner.EncryptionKey.EncryptedKey,
                encryptionSettings.Key
            );

            return await aesEncryptionService.Decrypt(
                car.Owner.EncryptedLicenseNumber,
                ownerKey,
                car.Owner.EncryptionKey.IV
            );
        }
    }
}
