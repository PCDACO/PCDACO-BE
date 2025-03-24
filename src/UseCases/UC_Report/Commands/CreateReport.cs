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
using UUIDNext;

namespace UseCases.UC_Report.Commands;

public sealed class CreateReport
{
    public record Command(
        Guid BookingId,
        string Title,
        string Description,
        BookingReportType ReportType,
        IFormFile[] Images
    ) : IRequest<Result<Response>>;

    public record Response(Guid Id)
    {
        public static Response FromEntity(BookingReport report) => new(report.Id);
    }

    internal class Handler(
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
            // Check if booking exists and user has access
            var booking = await context
                .Bookings.Include(b => b.Car)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking is null)
                return Result.NotFound(ResponseMessages.BookingNotFound);

            // Check if user has permission to report
            if (
                !currentUser.User!.IsAdmin()
                && currentUser.User.Id != booking.UserId
                && currentUser.User.Id != booking.Car.OwnerId
            )
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            var report = new BookingReport
            {
                BookingId = request.BookingId,
                ReportedById = currentUser.User.Id,
                Title = request.Title,
                Description = request.Description,
                ReportType = request.ReportType,
                Status = BookingReportStatus.Pending
            };

            // Upload images if any
            if (request.Images?.Length > 0)
            {
                var imageTasks = request.Images.Select(image =>
                    cloudinaryServices.UploadReportImageAsync(
                        $"Report-{report.Id}-Image-{Uuid.NewRandom()}",
                        image.OpenReadStream(),
                        cancellationToken
                    )
                );

                var imageUrls = await Task.WhenAll(imageTasks);

                report.ImageReports = [.. imageUrls.Select(url => new ImageReport { Url = url })];
            }

            await context.BookingReports.AddAsync(report, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(report), ResponseMessages.Created);
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BookingId).NotEmpty().WithMessage("Phải chọn đơn đặt xe cần báo cáo");

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

            RuleFor(x => x.ReportType).IsInEnum().WithMessage("Loại báo cáo không hợp lệ");

            RuleFor(x => x.Images)
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
