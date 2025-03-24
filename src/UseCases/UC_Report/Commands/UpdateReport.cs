using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;
using UUIDNext;

namespace UseCases.UC_Report.Commands;

public sealed class UpdateReport
{
    public record Command(
        Guid Id,
        string Title,
        string Description,
        BookingReportStatus Status,
        IFormFile[]? NewImages
    ) : IRequest<Result<Response>>;

    public record Response(Guid Id)
    {
        public static Response FromEntity(BookingReport report) => new(report.Id);
    }

    public class Handler(
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
            var report = await context
                .BookingReports.Include(r => r.Booking)
                .ThenInclude(b => b.Car)
                .Include(r => r.ImageReports)
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

            if (report is null)
                return Result.NotFound(ResponseMessages.ReportNotFound);

            // Check if user has permission to update
            if (
                !currentUser.User!.IsAdmin()
                && !currentUser.User.IsConsultant()
                && currentUser.User.Id != report.ReportedById
            )
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            var reportTime = GetTimestampFromUuid.Execute(report.Id);

            if (reportTime.AddHours(24) < DateTimeOffset.UtcNow)
                return Result.Error("Không thể cập nhật báo cáo sau 24 giờ");

            report.Title = request.Title;
            report.Description = request.Description;
            report.Status = request.Status;
            report.UpdatedAt = DateTimeOffset.UtcNow;

            // Handle new images if any
            if (request.NewImages?.Length > 0)
            {
                // Delete existing images
                context.ImageReports.RemoveRange(report.ImageReports);

                var imageTasks = request.NewImages.Select(image =>
                    cloudinaryServices.UploadReportImageAsync(
                        $"Report-{report.Id}-Image-{Uuid.NewRandom()}",
                        image.OpenReadStream(),
                        cancellationToken
                    )
                );

                var imageUrls = await Task.WhenAll(imageTasks);

                report.ImageReports = [.. imageUrls.Select(url => new ImageReport { Url = url })];
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(report), ResponseMessages.Updated);
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("ID báo cáo không được để trống");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Tiêu đề không được để trống")
                .MaximumLength(100)
                .WithMessage("Tiêu đề không được quá 100 ký tự");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Mô tả không được để trống")
                .MaximumLength(1000)
                .WithMessage("Mô tả không được quá 1000 ký tự");

            RuleFor(x => x.Status).IsInEnum().WithMessage("Trạng thái báo cáo không hợp lệ");

            RuleFor(x => x.NewImages)
                .Must(ValidateImages)
                .WithMessage(
                    "Ảnh không được vượt quá 10MB và chỉ chấp nhận định dạng: jpg, jpeg, png"
                );
        }

        private static bool ValidateImages(IFormFile[]? images)
        {
            if (images == null)
                return true;

            return images.All(image =>
                image.Length <= 10 * 1024 * 1024
                && // 10MB
                (
                    image.ContentType == "image/jpeg"
                    || image.ContentType == "image/jpg"
                    || image.ContentType == "image/png"
                )
            );
        }
    }
}
