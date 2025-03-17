using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UUIDNext;

namespace UseCases.UC_Booking.Commands;

public sealed class InspectionImages
{
    public sealed record Command(
        Guid BookingId,
        InspectionType InspectionType,
        InspectionPhotoType InspectionPhotoType,
        Stream[] Images,
        string Note = ""
    ) : IRequest<Result<Response>>;

    public sealed record Response(ImageDetail[] Images)
    {
        public static Response FromEntity(Guid id, string[] urls) =>
            new([.. urls.Select(i => new ImageDetail(id, i))]);
    };

    public record ImageDetail(Guid Id, string Url);

    internal sealed class Handler(
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
            if (!currentUser.User!.IsOwner())
                return Result.Forbidden(ResponseMessages.UnauthourizeAccess);

            var booking = await context
                .Bookings.AsNoTracking()
                .Include(b => b.CarInspections.Where(i => i.Type == request.InspectionType))
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound(ResponseMessages.BookingNotFound);

            if (
                booking.Status != BookingStatusEnum.Approved
                && booking.Status != BookingStatusEnum.Completed
            )
                return Result.Error("Không thể tìm thấy đặt xe hoặc đặt xe không hợp lệ!");

            if (
                booking.StartTime > DateTime.Now.AddHours(24)
                && request.InspectionType == InspectionType.PreBooking
            )
                return Result.Error(
                    "Chỉ có thể gửi hình ảnh kiểm tra trong vòng 24 giờ trước thời gian bắt đầu!"
                );

            // Upload each image to Cloudinary
            List<Task<string>> uploadTasks = [];

            foreach (var image in request.Images)
            {
                string fileName =
                    $"Inspection-{booking.Id}-{request.InspectionType}-{request.InspectionPhotoType}";

                uploadTasks.Add(
                    cloudinaryServices.UploadBookingInspectionImageAsync(
                        fileName,
                        image,
                        cancellationToken
                    )
                );
            }

            var uploadResults = await Task.WhenAll(uploadTasks);

            // Get or create inspection record
            var carInspectionId = await GetOrCreateInspectionRecord(
                booking,
                request.InspectionType,
                cancellationToken
            );

            // Create inspection photos
            InspectionPhoto[] inspectionPhotos =
            [
                .. uploadResults.Select(url => new InspectionPhoto
                {
                    InspectionId = carInspectionId,
                    Type = request.InspectionPhotoType,
                    PhotoUrl = url,
                    Description = request.Note
                })
            ];

            await context.InspectionPhotos.AddRangeAsync(inspectionPhotos, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                Response.FromEntity(carInspectionId, uploadResults),
                ResponseMessages.Created
            );
        }

        private async Task<Guid> GetOrCreateInspectionRecord(
            Booking booking,
            InspectionType inspectionType,
            CancellationToken cancellationToken
        )
        {
            var existingInspection = booking.CarInspections.FirstOrDefault(i =>
                i.Type == inspectionType
            );

            if (existingInspection != null)
                return existingInspection.Id;

            var newInspectionId = Uuid.NewDatabaseFriendly(Database.PostgreSql);
            var carInspection = new CarInspection
            {
                Id = newInspectionId,
                BookingId = booking.Id,
                Type = inspectionType,
                IsComplete = false // Will be set to true when all required photos are uploaded
            };

            await context.CarInspections.AddAsync(carInspection, cancellationToken);
            return newInspectionId;
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BookingId).NotEmpty().WithMessage("Yêu cầu mã đặt xe");

            // RuleFor(x => x.Images).NotEmpty().WithMessage("Yêu cầu hình ảnh kiểm tra");
        }
    }
}
