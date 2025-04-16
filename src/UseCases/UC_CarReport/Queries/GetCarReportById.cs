using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_CarReport.Queries;

public class GetCarReportById
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid Id,
        Guid ReporterId,
        string ReporterName,
        string ReporterRole,
        string Title,
        string Description,
        CarReportType ReportType,
        CarReportStatus Status,
        DateTimeOffset? ResolvedAt,
        Guid? ResolvedById,
        string? ResolutionComments,
        string[] ImageUrls,
        CarDetail CarDetail,
        InspectionScheduleDetail? InspectionScheduleDetail
    )
    {
        public static async Task<Response> FromEntity(
            CarReport report,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            // Decrypt owner phone
            string ownerDecryptedKey = keyManagementService.DecryptKey(
                report.Car.Owner.EncryptionKey.EncryptedKey,
                masterKey
            );
            string ownerPhone = await aesEncryptionService.Decrypt(
                report.Car.Owner.Phone,
                ownerDecryptedKey,
                report.Car.Owner.EncryptionKey.IV
            );

            return new(
                report.Id,
                report.ReportedById,
                report.ReportedBy.Name,
                report.ReportedBy.Role.Name,
                report.Title,
                report.Description,
                report.ReportType,
                report.Status,
                report.ResolvedAt,
                report.ResolvedById,
                report.ResolutionComments,
                [.. report.ImageReports.Select(i => i.Url)],
                new CarDetail(
                    report.Car.Id,
                    report.Car.LicensePlate,
                    report.Car.Model.Name,
                    report.Car.Model.Manufacturer.Name,
                    report.Car.Color,
                    report.Car.OwnerId,
                    report.Car.Owner.Name,
                    report.Car.Owner.AvatarUrl,
                    ownerPhone,
                    [.. report.Car.ImageCars.Select(i => i.Url)]
                ),
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
    }

    public sealed record CarDetail(
        Guid Id,
        string LicensePlate,
        string ModelName,
        string ManufacturerName,
        string Color,
        Guid OwnerId,
        string OwnerName,
        string OwnerAvatar,
        string OwnerPhone,
        string[] ImageUrls
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
            CarReport? report = await context
                .CarReports.Include(r => r.ReportedBy)
                .ThenInclude(r => r.Role)
                .Include(r => r.ImageReports)
                .Include(r => r.Car)
                .ThenInclude(c => c.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(r => r.Car.Model)
                .ThenInclude(m => m.Manufacturer)
                .Include(r => r.Car.ImageCars)
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
                && report.Car.OwnerId != currentUser.User.Id // Owner can only view reports related to their cars
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
