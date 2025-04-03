using System.Text;
using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_InspectionSchedule.Commands;

public sealed class UploadInspectionSchedulePhotos
{
    public sealed record Command(
        Guid InspectionScheduleId,
        InspectionPhotoType PhotoType,
        Stream[] PhotoFiles,
        string Description = "",
        DateTimeOffset? ExpiryDate = null
    ) : IRequest<Result<Response>>;

    public sealed record Response(ImageDetail[] Images)
    {
        public static Response FromEntity(Guid inspectionScheduleId, InspectionPhoto[] photos) =>
            new([.. photos.Select(p => new ImageDetail(inspectionScheduleId, p.PhotoUrl))]);
    };

    public record ImageDetail(Guid InspectionScheduleId, string Url);

    public sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        ICloudinaryServices cloudinaryServices
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Check if user is technician
            if (!currentUser.User!.IsTechnician())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");

            // Check if inspection schedule exists
            var inspectionSchedule = await context
                .InspectionSchedules.AsNoTracking()
                .FirstOrDefaultAsync(
                    i => i.Id == request.InspectionScheduleId && !i.IsDeleted,
                    cancellationToken
                );

            if (inspectionSchedule is null)
                return Result.NotFound("Không tìm thấy thông tin kiểm định xe");

            // Check if the inspection schedule is in a valid status for uploading photos
            if (
                inspectionSchedule.Status == InspectionScheduleStatusEnum.Pending
                || inspectionSchedule.Status == InspectionScheduleStatusEnum.Expired
            )
            {
                return Result.Error(
                    "Chỉ có thể tải lên ảnh kiểm định sau khi đã bắt đầu kiểm định và trước khi bị quá hạn kiểm định"
                );
            }

            // Check if the inspection schedule is assigned to the current user
            if (inspectionSchedule.TechnicianId != currentUser.User.Id)
            {
                return Result.Forbidden("Bạn không phải là kiểm định viên được chỉ định");
            }

            // Upload new images
            List<Task<string>> uploadTasks = [];

            foreach (var photo in request.PhotoFiles)
            {
                string fileName =
                    $"Inspection-{inspectionSchedule.Id}-{request.PhotoType}-{Guid.NewGuid()}";
                uploadTasks.Add(
                    cloudinaryServices.UploadBookingInspectionImageAsync(
                        fileName,
                        photo,
                        cancellationToken
                    )
                );
            }

            var uploadResults = await Task.WhenAll(uploadTasks);

            // Create inspection photos
            InspectionPhoto[] inspectionPhotos =
            [
                .. uploadResults.Select(url => new InspectionPhoto
                {
                    ScheduleId = inspectionSchedule.Id,
                    Type = request.PhotoType,
                    PhotoUrl = url,
                    Description = request.Description,
                    InspectionCertificateExpiryDate =
                        request.PhotoType == InspectionPhotoType.VehicleInspectionCertificate
                            ? request.ExpiryDate
                            : null,
                }),
            ];

            await context.InspectionPhotos.AddRangeAsync(inspectionPhotos, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                Response.FromEntity(inspectionSchedule.Id, inspectionPhotos),
                "Tải lên ảnh kiểm định thành công"
            );
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        private const int MaxFileSizeInMb = 10;
        private readonly string[] allowedExtensions =
        [
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".bmp",
            ".tiff",
            ".webp",
            ".svg",
            ".heic",
            ".heif",
        ];

        public Validator()
        {
            RuleFor(x => x.InspectionScheduleId)
                .NotEmpty()
                .WithMessage("ID kiểm định không được để trống");

            RuleFor(x => x.PhotoFiles).NotEmpty().WithMessage("Yêu cầu ít nhất một ảnh kiểm định");

            RuleForEach(x => x.PhotoFiles)
                .Must(ValidateFileSize)
                .WithMessage($"Kích thước ảnh không được vượt quá {MaxFileSizeInMb}MB")
                .Must(ValidateFileType)
                .WithMessage(
                    $"Chỉ chấp nhận các định dạng: {string.Join(", ", allowedExtensions)}"
                );

            When(
                x => x.PhotoType == InspectionPhotoType.VehicleInspectionCertificate,
                () =>
                    RuleFor(x => x.ExpiryDate)
                        .NotEmpty()
                        .WithMessage("Ngày hết hạn giấy kiểm định không được để trống")
                        .Must(date => date >= DateTimeOffset.UtcNow)
                        .WithMessage(
                            "Ngày hết hạn giấy kiểm định phải lớn hơn hoặc bằng thời điểm hiện tại"
                        )
            );
        }

        private bool ValidateFileSize(Stream file)
        {
            return file?.Length <= MaxFileSizeInMb * 1024 * 1024;
        }

        private bool ValidateFileType(Stream file)
        {
            if (file == null)
                return false;

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                fileBytes = memoryStream.ToArray();
                file.Position = 0; // Reset stream position
            }

            return IsValidImageFile(fileBytes);
        }

        private bool IsValidImageFile(byte[] fileBytes)
        {
            if (fileBytes.Length < 4)
                return false;

            return fileBytes[..2].SequenceEqual(new byte[] { 0xFF, 0xD8 })
                || // JPEG and JPG
                fileBytes[..4].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 })
                || // PNG
                fileBytes[..3].SequenceEqual(new byte[] { 0x47, 0x49, 0x46 })
                || // GIF
                fileBytes[..2].SequenceEqual(new byte[] { 0x42, 0x4D })
                || // BMP
                fileBytes[..4].SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 })
                || // WebP
                fileBytes[..4].SequenceEqual(new byte[] { 0x49, 0x49, 0x2A, 0x00 })
                || // TIFF (Little-endian)
                fileBytes[..4].SequenceEqual(new byte[] { 0x4D, 0x4D, 0x00, 0x2A })
                || // TIFF (Big-endian)
                Encoding.UTF8.GetString(fileBytes).Contains("<svg")
                || // SVG
                fileBytes.Length >= 12
                    && fileBytes[4] == 0x66
                    && fileBytes[5] == 0x74
                    && fileBytes[6] == 0x79
                    && fileBytes[7] == 0x70
                    && (
                        fileBytes[8] == 0x68
                            && fileBytes[9] == 0x65
                            && fileBytes[10] == 0x69
                            && fileBytes[11] == 0x63
                        || // HEIC
                        fileBytes[8] == 0x68
                            && fileBytes[9] == 0x65
                            && fileBytes[10] == 0x69
                            && fileBytes[11] == 0x66
                    ); // HEIF
        }
    }
}
