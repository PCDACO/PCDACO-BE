using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
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
        public sealed record Query(int PageNumber = 1, int PageSize = 10, string SortOrder = "desc")
            : IRequest<Result<OffsetPaginatedResponse<Response>>>;

        // Updated response records
        public sealed record Response(
            string TechnicianName,
            DateTimeOffset InspectionDate,
            string InspectionAddress,
            CarDetail[] Cars
        )
        {
            public static async Task<Response> FromEntity(
                InspectionSchedule schedule,
                string masterKey,
                IAesEncryptionService aesEncryptionService,
                IKeyManagementService keyManagementService
            )
            {
                string decryptedKey = keyManagementService.DecryptKey(
                    schedule.Car.EncryptionKey.EncryptedKey,
                    masterKey
                );

                string decryptedLicensePlate = await aesEncryptionService.Decrypt(
                    schedule.Car.EncryptedLicensePlate,
                    decryptedKey,
                    schedule.Car.EncryptionKey.IV
                );

                return new(
                    schedule.Technician.Name,
                    schedule.InspectionDate,
                    schedule.InspectionAddress,
                    new[]
                    {
                        new CarDetail(
                            schedule.Car.Id,
                            schedule.Car.Model.Id,
                            schedule.Car.Model.Name,
                            schedule.Car.Model.Manufacturer.Name,
                            decryptedLicensePlate,
                            schedule.Car.Color,
                            schedule.Car.Seat,
                            schedule.Car.Description,
                            schedule.Car.TransmissionType.Name,
                            schedule.Car.FuelType.Name,
                            schedule.Car.FuelConsumption,
                            schedule.Car.RequiresCollateral,
                            schedule.Car.Price,
                            schedule
                                .Car.ImageCars.Select(i => new ImageDetail(i.Id, i.Url))
                                .ToArray(),
                            new UserDetail(
                                schedule.Car.Owner.Id,
                                schedule.Car.Owner.Name,
                                schedule.Car.Owner.AvatarUrl
                            )
                        ),
                    }
                );
            }
        };

        public record CarDetail(
            Guid Id,
            Guid ModelId,
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
            UserDetail Owner
        );

        public record ImageDetail(Guid Id, string Url);

        public record UserDetail(Guid Id, string Name, string AvatarUrl);

        internal sealed class Handler(
            IAppDBContext context,
            CurrentUser currentUser,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService,
            EncryptionSettings encryptionSettings
        ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
        {
            public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
                Query request,
                CancellationToken cancellationToken
            )
            {
                if (!currentUser.User!.IsTechnician())
                    return Result.Forbidden(ResponseMessages.ForbiddenAudit);

                var today = DateTimeOffset.UtcNow.Date;
                var query = context
                    .InspectionSchedules.AsSplitQuery()
                    .Include(s => s.InspectionStatus)
                    .Include(s => s.Technician)
                    .Include(s => s.Car)
                    .ThenInclude(c => c.Model)
                    .ThenInclude(m => m.Manufacturer)
                    .Include(s => s.Car)
                    .ThenInclude(c => c.ImageCars)
                    .Include(s => s.Car)
                    .ThenInclude(c => c.Owner)
                    .Include(s => s.Car)
                    .ThenInclude(c => c.EncryptionKey)
                    .Include(s => s.Car)
                    .ThenInclude(c => c.TransmissionType)
                    .Include(s => s.Car)
                    .ThenInclude(c => c.FuelType)
                    .Where(s =>
                        s.TechnicianId == currentUser.User.Id
                        && EF.Functions.ILike(s.InspectionStatus.Name, "%pending%")
                        && !s.IsDeleted
                        && s.InspectionDate.Date == today
                    );

                // Apply sorting
                query =
                    request.SortOrder.ToLower() == "asc"
                        ? query.OrderBy(s => s.Id)
                        : query.OrderByDescending(s => s.Id);

                //Get Total Count
                var count = await query.CountAsync(cancellationToken);

                //Get Paginated Data
                var schedulesData = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                //Map To Schedule Response
                var schedules = await Task.WhenAll(
                    schedulesData.Select(async schedule =>
                        await Response.FromEntity(
                            schedule,
                            encryptionSettings.Key,
                            aesEncryptionService,
                            keyManagementService
                        )
                    )
                );

                // Check if there are more items
                var hasNext = await query
                    .Skip(request.PageNumber * request.PageSize)
                    .AnyAsync(cancellationToken);

                return Result.Success(
                    new OffsetPaginatedResponse<Response>(
                        [.. schedules],
                        count,
                        request.PageNumber,
                        request.PageSize,
                        hasNext
                    ),
                    ResponseMessages.Fetched
                );
            }
        }
    }
}
