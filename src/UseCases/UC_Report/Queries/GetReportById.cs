using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Report.Queries;

public class GetReportById
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid Id,
        Guid ReporterId,
        string ReporterName,
        string ReporterRole,
        string Title,
        string Description,
        BookingReportType ReportType,
        BookingReportStatus Status,
        DateTimeOffset? ResolvedAt,
        Guid? ResolvedById,
        string? ResolutionComments,
        string[] ImageUrls,
        BookingDetail BookingDetail,
        CarDetail CarDetail,
        CompensationDetail? CompensationDetail,
        InspectionScheduleDetail? InspectionScheduleDetail
    )
    {
        public static async Task<Response> FromEntity(
            BookingReport report,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            string decryptedLicensePlate = await DecryptLicensePlate(
                report,
                masterKey,
                aesEncryptionService,
                keyManagementService
            );

            var decryptedPhones = await DecryptUserPhones(
                report,
                masterKey,
                aesEncryptionService,
                keyManagementService
            );

            return new(
                report.Id,
                report.ReportedById,
                report.ReportedBy.Name,
                report.Booking.User.Role.Name,
                report.Title,
                report.Description,
                report.ReportType,
                report.Status,
                report.ResolvedAt,
                report.ResolvedById,
                report.ResolutionComments,
                [.. report.ImageReports.Select(i => i.Url)],
                new BookingDetail(
                    report.Booking.Id,
                    report.Booking.UserId,
                    report.Booking.User.Name,
                    report.Booking.User.AvatarUrl,
                    decryptedPhones.DriverPhone,
                    report.Booking.Car.OwnerId,
                    report.Booking.Car.Owner.Name,
                    report.Booking.Car.Owner.AvatarUrl,
                    decryptedPhones.OwnerPhone,
                    report.Booking.StartTime,
                    report.Booking.EndTime,
                    report.Booking.TotalAmount,
                    report.Booking.BasePrice
                ),
                new CarDetail(
                    report.Booking.Car.Id,
                    decryptedLicensePlate,
                    report.Booking.Car.Model.Name,
                    report.Booking.Car.Model.Manufacturer.Name,
                    report.Booking.Car.Color,
                    [.. report.Booking.Car.ImageCars.Select(i => i.Url)]
                ),
                report.CompensationPaidUserId != null
                    ? new CompensationDetail(
                        report.CompensationPaidUserId,
                        report.CompensationPaidUser?.Name,
                        report.CompensationPaidUser?.AvatarUrl,
                        report.CompensationReason,
                        report.CompensationAmount,
                        report.IsCompensationPaid,
                        report.CompensationPaidImageUrl,
                        report.CompensationPaidAt
                    )
                    : null,
                report.InspectionSchedule != null
                    ? new InspectionScheduleDetail(
                        report.InspectionSchedule.Id,
                        report.InspectionSchedule.TechnicianId,
                        report.InspectionSchedule.Technician?.Name,
                        report.InspectionSchedule.Technician?.AvatarUrl,
                        report.InspectionSchedule.Status,
                        report.InspectionSchedule.InspectionAddress,
                        report.InspectionSchedule.InspectionDate,
                        report.InspectionSchedule.Note,
                        [.. report.InspectionSchedule.Photos.Select(p => p.PhotoUrl)]
                    )
                    : null
            );
        }

        private static async Task<string> DecryptLicensePlate(
            BookingReport report,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            string decryptedKey = keyManagementService.DecryptKey(
                report.Booking.Car.EncryptionKey.EncryptedKey,
                masterKey
            );

            return await aesEncryptionService.Decrypt(
                report.Booking.Car.EncryptedLicensePlate,
                decryptedKey,
                report.Booking.Car.EncryptionKey.IV
            );
        }

        private static async Task<(string DriverPhone, string OwnerPhone)> DecryptUserPhones(
            BookingReport report,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            // Decrypt driver phone
            string driverDecryptedKey = keyManagementService.DecryptKey(
                report.Booking.User.EncryptionKey.EncryptedKey,
                masterKey
            );
            string driverPhone = await aesEncryptionService.Decrypt(
                report.Booking.User.Phone,
                driverDecryptedKey,
                report.Booking.User.EncryptionKey.IV
            );

            // Decrypt owner phone
            string ownerDecryptedKey = keyManagementService.DecryptKey(
                report.Booking.Car.Owner.EncryptionKey.EncryptedKey,
                masterKey
            );
            string ownerPhone = await aesEncryptionService.Decrypt(
                report.Booking.Car.Owner.Phone,
                ownerDecryptedKey,
                report.Booking.Car.Owner.EncryptionKey.IV
            );

            return (driverPhone, ownerPhone);
        }
    }

    public sealed record CompensationDetail(
        Guid? UserId,
        string? UserName,
        string? UserAvatar,
        string? CompensationReason,
        decimal? CompensationAmount,
        bool? IsPaid,
        string? ImageUrl,
        DateTimeOffset? PaidAt
    );

    public sealed record BookingDetail(
        Guid Id,
        Guid DriverId,
        string DriverName,
        string DriverAvatar,
        string DriverPhone,
        Guid OwnerId,
        string OwnerName,
        string OwnerAvatar,
        string OwnerPhone,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
        decimal TotalAmount,
        decimal BasePrice
    );

    public sealed record CarDetail(
        Guid Id,
        string LicensePlate,
        string ModelName,
        string ManufacturerName,
        string Color,
        string[] ImageUrl
    );

    public sealed record InspectionScheduleDetail(
        Guid? Id,
        Guid? TechnicianId,
        string? TechnicianName,
        string? TechnicianAvatar,
        InspectionScheduleStatusEnum? Status,
        string? InspectionAddress,
        DateTimeOffset? InspectionDate,
        string? Note,
        string[]? PhotoUrls
    );

    private sealed class Handler(
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
            BookingReport? report = await context
                .BookingReports.Include(r => r.ReportedBy)
                .ThenInclude(r => r.Role)
                .Include(r => r.ImageReports)
                .Include(r => r.Booking)
                .ThenInclude(b => b.User)
                .ThenInclude(u => u.EncryptionKey)
                .Include(r => r.Booking.Car)
                .ThenInclude(c => c.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(r => r.Booking.Car.Model)
                .ThenInclude(m => m.Manufacturer)
                .Include(r => r.Booking.Car.ImageCars)
                .Include(r => r.Booking.Car.EncryptionKey)
                .Include(r => r.CompensationPaidUser)
                .Include(r => r.InspectionSchedule)
                .ThenInclude(i => i!.Technician)
                .Include(r => r.InspectionSchedule!.Photos)
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

            if (report is null)
                return Result.NotFound(ResponseMessages.ReportNotFound);

            // Validate access rights
            if (
                !currentUser.User!.IsAdmin() // Admin can view any report
                && !currentUser.User!.IsConsultant() // Consultant can view any report
                && report.Booking.UserId != currentUser.User.Id // Driver can only view their own reports
                && report.Booking.Car.OwnerId != currentUser.User.Id // Owner can only view reports related to their cars
                && report.ReportedById != currentUser.User.Id // Reporter can view their own reports
            )
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            return Result.Success(
                await Response.FromEntity(
                    report,
                    encryptionSettings.Key,
                    aesEncryptionService,
                    keyManagementService
                ),
                ResponseMessages.Fetched
            );
        }
    }
}
