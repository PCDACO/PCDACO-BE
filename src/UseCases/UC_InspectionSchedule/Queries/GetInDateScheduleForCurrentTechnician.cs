using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_InspectionSchedule.Queries
{
    public sealed class GetInDateScheduleForCurrentTechnician
    {
        // Query remains the same
        public sealed record Query(DateTimeOffset? InspectionDate = null, bool? IsIncident = null)
            : IRequest<Result<Response>>;

        // Updated response records
        public sealed record Response(
            string TechnicianName,
            DateTimeOffset InspectionDate,
            CarDetail[] Cars
        )
        {
            public static async Task<Response> FromEntity(
                string technicianName,
                DateTimeOffset inspectionDate,
                IEnumerable<InspectionSchedule> schedules,
                string masterKey,
                IAesEncryptionService aesEncryptionService,
                IKeyManagementService keyManagementService
            )
            {
                return new Response(
                    TechnicianName: technicianName,
                    InspectionDate: inspectionDate,
                    Cars: await Task.WhenAll(
                        schedules.Any()
                            ? schedules.Select(async schedule =>
                            {
                                return new CarDetail(
                                    Id: schedule.CarId,
                                    ModelId: schedule.Car.ModelId,
                                    InspectionScheduleId: schedule.Id,
                                    ModelName: schedule.Car.Model.Name,
                                    ManufacturerName: schedule.Car.Model.Manufacturer.Name,
                                    LicensePlate: schedule.Car.LicensePlate,
                                    Color: schedule.Car.Color,
                                    Seat: schedule.Car.Seat,
                                    Description: schedule.Car.Description,
                                    TransmissionType: schedule.Car.TransmissionType.Name,
                                    FuelType: schedule.Car.FuelType.Name,
                                    FuelConsumption: schedule.Car.FuelConsumption,
                                    RequiresCollateral: schedule.Car.RequiresCollateral,
                                    Price: schedule.Car.Price,
                                    Images:
                                    [
                                        .. schedule.Car.ImageCars.Select(image => new ImageDetail(
                                            Id: image.Id,
                                            Url: image.Url,
                                            ImageTypeName: image.Type.Name
                                        )),
                                    ],
                                    Owner: new(
                                        Id: schedule.Car.Owner.Id,
                                        Name: schedule.Car.Owner.Name,
                                        AvatarUrl: schedule.Car.Owner.AvatarUrl
                                    ),
                                    InspectionAddress: schedule.InspectionAddress
                                );
                            })
                            : []
                    )
                );
            }
        }

        public record CarDetail(
            Guid Id,
            Guid ModelId,
            Guid InspectionScheduleId,
            string ModelName,
            string ManufacturerName,
            string LicensePlate,
            string Color,
            int Seat,
            string Description,
            string TransmissionType,
            string FuelType,
            decimal FuelConsumption,
            bool RequiresCollateral,
            decimal Price,
            ImageDetail[] Images,
            UserDetail Owner,
            string InspectionAddress
        );

        public record ImageDetail(Guid Id, string Url, string ImageTypeName);

        public record UserDetail(Guid Id, string Name, string AvatarUrl);

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
                if (!currentUser.User!.IsTechnician())
                    return Result.Forbidden(ResponseMessages.ForbiddenAudit);
                var inspectionDate = request.InspectionDate switch
                {
                    not null => request.InspectionDate!.Value,
                    _ => DateTimeOffset.UtcNow,
                };
                InspectionScheduleType scheduleType =
                    request.IsIncident == true
                        ? InspectionScheduleType.Incident
                        : InspectionScheduleType.NewCar;
                IEnumerable<InspectionSchedule> schedules = await context
                    .InspectionSchedules.AsNoTracking()
                    .AsSplitQuery()
                    .Include(s => s.Technician)
                    .Include(s => s.Car)
                    .ThenInclude(c => c.Model)
                    .ThenInclude(m => m.Manufacturer)
                    .Include(s => s.Car)
                    .ThenInclude(c => c.ImageCars)
                    .ThenInclude(i => i.Type)
                    .Include(s => s.Car)
                    .ThenInclude(c => c.Owner)
                    .Include(s => s.Car)
                    .ThenInclude(c => c.TransmissionType)
                    .Include(s => s.Car)
                    .ThenInclude(c => c.FuelType)
                    .Where(s => s.Status == InspectionScheduleStatusEnum.Pending)
                    .Where(s =>
                        s.TechnicianId == currentUser.User.Id
                        && !s.IsDeleted
                        && s.InspectionDate.Date == inspectionDate.Date
                        && (s.Type == scheduleType || request.IsIncident == null)
                    )
                    .OrderBy(s => s.Id)
                    .ToListAsync(cancellationToken);

                return Result.Success(
                    await Response.FromEntity(
                        currentUser.User.Name,
                        inspectionDate,
                        schedules,
                        encryptionSettings.Key,
                        aesEncryptionService,
                        keyManagementService
                    ),
                    ResponseMessages.Fetched
                );
            }
        }
    }
}
