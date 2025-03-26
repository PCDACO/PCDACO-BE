using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UUIDNext;

namespace UseCases.UC_Report.Commands;

public sealed class UploadReportImages
{
    public record Command(Guid ReportId, ImageFile[] Images) : IRequest<Result<Response>>;

    public record ImageFile
    {
        public required Stream Content { get; set; }
        public required string FileName { get; set; }
    }

    public record Response(ImageDetail[] Images)
    {
        public static Response FromEntity(Guid id, ImageReport[] files)
        {
            return new Response([.. files.Select(f => new ImageDetail(id, f.Url))]);
        }
    }

    public record ImageDetail(Guid Id, string Url);

    internal class Handler(
        IAppDBContext context,
        ICloudinaryServices cloudinaryServices,
        CurrentUser currentUser
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
                .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

            if (report is null)
                return Result.NotFound(ResponseMessages.ReportNotFound);

            // Check if user has permission
            if (
                currentUser.User!.Id != report.ReportedById
                && currentUser.User.Id != report.Booking.UserId
                && currentUser.User.Id != report.Booking.Car.OwnerId
            )
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Remove existing images if any
            if (report.ImageReports.Any())
            {
                context.ImageReports.RemoveRange(report.ImageReports);
            }

            // Upload new images
            var imageTasks = request.Images.Select(image =>
                cloudinaryServices.UploadReportImageAsync(
                    $"Report-{report.Id}-Image-{Uuid.NewRandom()}",
                    image.Content,
                    cancellationToken
                )
            );

            var imageUrls = await Task.WhenAll(imageTasks);

            var newImages = imageUrls
                .Select(url => new ImageReport { BookingReportId = report.Id, Url = url })
                .ToArray();

            await context.ImageReports.AddRangeAsync(newImages, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                Response.FromEntity(report.Id, newImages),
                ResponseMessages.Updated
            );
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png"];

        public Validator()
        {
            RuleFor(x => x.ReportId).NotEmpty().WithMessage("Phải chọn báo cáo cần cập nhật ảnh");

            RuleFor(x => x.Images)
                .NotEmpty()
                .WithMessage("Phải chọn ít nhất một ảnh")
                .Must(ValidateImages)
                .WithMessage(
                    "Ảnh không được vượt quá 10MB và chỉ chấp nhận định dạng: jpg, jpeg, png"
                );
        }

        private static bool ValidateImages(ImageFile[]? images)
        {
            if (images == null)
                return false;

            return images.All(image =>
                image.Content.Length <= 10 * 1024 * 1024 // 10MB
                && AllowedImageExtensions.Contains(
                    Path.GetExtension(image.FileName).ToLowerInvariant()
                )
            );
        }
    }
}
