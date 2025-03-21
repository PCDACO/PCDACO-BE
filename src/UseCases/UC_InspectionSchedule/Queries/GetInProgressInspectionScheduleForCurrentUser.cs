using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_InspectionSchedule.Queries;

public class GetInProgressInspectionScheduleForCurrentUser
{
    public record Query() : IRequest<Result<Response>>;

    public record Response(
            Guid Id,
            DateTimeOffset Date,
            string Address,
            string Notes,
            TechnicianDetail Technician,
            OwnerDetail Owner,
            CarDetail Car,
            DateTimeOffset CreatedAt
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
                inspectionSchedule.Car.Owner.EncryptionKey.EncryptedKey,
                masterKey
            );
            string decryptedPhone = await aesEncryptionService.Decrypt(
                inspectionSchedule.Car.Owner.Phone,
                decryptedKey,
                inspectionSchedule.Car.Owner.EncryptionKey.IV
            );
            return new(
                    Id: inspectionSchedule.Id,
                    Date: inspectionSchedule.InspectionDate,
                    Address: inspectionSchedule.InspectionAddress,
                    Notes: inspectionSchedule.Note,
                    Technician: new(
                        Id: inspectionSchedule.Technician.Id,
                        Name: inspectionSchedule.Technician.Name
                        ),
                    Owner: new(
                        Id: inspectionSchedule.Car.Owner.Id,
                        Name: inspectionSchedule.Car.Owner.Name,
                        AvatarUrl: inspectionSchedule.Car.Owner.AvatarUrl,
                        Phone: decryptedPhone
                        ),
                    Car: new CarDetail(
                        Id: inspectionSchedule.Car.Id,
                        ModelId: inspectionSchedule.Car.Model.Id,
                        ModelName: inspectionSchedule.Car.Model.Name,
                        FuelType: inspectionSchedule.Car.FuelType.Name,
                        TransmissionType: inspectionSchedule.Car.TransmissionType.Name,
                        Amenities: inspectionSchedule.Car.CarAmenities
                            .Select(ca => new AmenityDetail(
                                Id: ca.Amenity.Id,
                                Name: ca.Amenity.Name,
                                IconUrl: ca.Amenity.IconUrl
                            )).ToArray()
                    ),
                    CreatedAt: GetTimestampFromUuid.Execute(inspectionSchedule.Id)
                  );
        }
    };

    public record CarDetail(
            Guid Id,
            Guid ModelId,
            string ModelName,
            string FuelType,
            string TransmissionType,
            AmenityDetail[] Amenities
            );

    public record TechnicianDetail(
            Guid Id,
            string Name
            );

    public record OwnerDetail(
            Guid Id,
            string Name,
            string AvatarUrl,
            string Phone
            );

    public record AmenityDetail(
            Guid Id,
            string Name,
            string IconUrl
    );

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService,
            EncryptionSettings encryptionSettings
            ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (currentUser.User is null)
            {
                return Result.Unauthorized(ResponseMessages.UnauthourizeAccess);
            }
            InspectionSchedule? result = await context.InspectionSchedules
               .AsNoTracking()
               .AsSplitQuery()
               .Include(i => i.Car).ThenInclude(c => c.Owner).ThenInclude(o => o.EncryptionKey)
               .Include(i => i.Car).ThenInclude(c => c.CarAmenities).ThenInclude(ca => ca.Amenity)
               .Include(i => i.Car).ThenInclude(c => c.FuelType)
               .Include(i => i.Car).ThenInclude(c => c.TransmissionType)
               .Include(i => i.Car).ThenInclude(c => c.Model)
               .Include(i => i.Technician)
               .Where(i => !i.IsDeleted)
               .Where(i => i.Status == Domain.Enums.InspectionScheduleStatusEnum.InProgress)
               .Where(i => i.TechnicianId == currentUser.User!.Id)
               .FirstOrDefaultAsync(cancellationToken);
            if (result is null)
            {
                return Result.Error(ResponseMessages.InspectionScheduleNotFound);
            }
            return Result.Success(await Response.FromEntity(
            result,
            encryptionSettings.Key,
            aesEncryptionService,
            keyManagementService
            ), ResponseMessages.Fetched);
        }
    }
}