using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_InspectionSchedule.Queries;

public class GetInProgressInspectionScheduleForCurrentUser
{
    public record Query() : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        DateTimeOffset Date,
        string OwnerName,
        string Address,
        string LicensePlate
    )
    {
        public static async Task<Response> FromEntity(
            InspectionSchedule inspectionSchedule,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            string decryptedKey = keyManagementService.DecryptKey(
                inspectionSchedule.Car.EncryptionKey.EncryptedKey,
                masterKey
            );
            string decryptedLicensePlate = await aesEncryptionService.Decrypt(
                inspectionSchedule.Car.EncryptedLicensePlate,
                decryptedKey,
                inspectionSchedule.Car.EncryptionKey.IV
            );
            return new(
                Id: inspectionSchedule.Id,
                Date: inspectionSchedule.InspectionDate,
                OwnerName: inspectionSchedule.Car.Owner.Name,
                Address: inspectionSchedule.InspectionAddress,
                LicensePlate: decryptedLicensePlate
            );
        }
    };

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
            if (currentUser.User is null)
            {
                return Result.Unauthorized(ResponseMessages.UnauthourizeAccess);
            }
            InspectionSchedule? result = await context
                .InspectionSchedules.AsNoTracking()
                .AsSplitQuery()
                .Include(i => i.Car)
                .ThenInclude(c => c.Owner)
                .Include(i => i.Car)
                .ThenInclude(c => c.EncryptionKey)
                .Include(i => i.Car)
                .ThenInclude(c => c.CarAmenities)
                .ThenInclude(ca => ca.Amenity)
                .Include(i => i.Car)
                .ThenInclude(c => c.FuelType)
                .Include(i => i.Car)
                .ThenInclude(c => c.TransmissionType)
                .Include(i => i.Car)
                .ThenInclude(c => c.Model)
                .Include(i => i.Technician)
                .Where(i => !i.IsDeleted)
                .Where(i => i.Status == Domain.Enums.InspectionScheduleStatusEnum.InProgress)
                .Where(i => i.TechnicianId == currentUser.User!.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (result is null)
            {
                return Result.NotFound(ResponseMessages.InspectionScheduleNotFound);
            }
            return Result.Success(
                await Response.FromEntity(
                    result,
                    encryptionSettings.Key,
                    aesEncryptionService,
                    keyManagementService
                ),
                ResponseMessages.Fetched
            );
        }
    }
}
