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

namespace UseCases.UC_Booking.Commands;

public sealed class PostInspectionImages
{
    public sealed record PhotoRequest(
        InspectionPhotoType Type,
        IFormFile[] Files,
        string Note = ""
    );

    public sealed record Command(Guid BookingId, List<PhotoRequest> Photos)
        : IRequest<Result<Response>>;

    public sealed record PhotoResponse(InspectionPhotoType Type, string[] Urls);

    public sealed record Response(Guid InspectionId, List<PhotoResponse> Photos);

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
                .Bookings.Include(b => b.Car)
                .Include(b => b.CarInspections.Where(i => i.Type == InspectionType.PostBooking))
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound(ResponseMessages.BookingNotFound);

            if (booking.Car.OwnerId != currentUser.User.Id)
                return Result.Forbidden("Bạn không có quyền phê duyệt booking cho xe này!");

            if (booking.Status != BookingStatusEnum.Completed)
                return Result.Error("Chỉ có thể kiểm tra xe sau khi kết thúc thuê xe!");

            // Validate required photo types
            if (!HasRequiredPhotoTypes(request.Photos.Select(p => p.Type)))
            {
                return Result.Error("Thiếu hình ảnh bắt buộc: mức xăng cuối và vệ sinh xe");
            }

            var existingInspection = booking.CarInspections.FirstOrDefault(i =>
                i.Type == InspectionType.PostBooking
            );

            if (existingInspection != null)
            {
                if (existingInspection.IsComplete)
                {
                    return Result.Error("Không thể cập nhật hình ảnh kiểm tra quá một lần!");
                }

                // Delete old photos
                var oldPhotos = await context
                    .InspectionPhotos.Where(i => i.InspectionId == existingInspection.Id)
                    .ToListAsync(cancellationToken);

                context.InspectionPhotos.RemoveRange(oldPhotos);
            }
            else
            {
                existingInspection = new CarInspection
                {
                    Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                    BookingId = booking.Id,
                    Type = InspectionType.PostBooking,
                    IsComplete = false
                };

                await context.CarInspections.AddAsync(existingInspection, cancellationToken);
            }

            var photoResponses = new List<PhotoResponse>();
            var allInspectionPhotos = new List<InspectionPhoto>();

            // Process each photo type
            foreach (var photoRequest in request.Photos)
            {
                var uploadTasks = photoRequest
                    .Files.Select(file =>
                    {
                        var fileName =
                            $"PostInspection-{booking.Id}-{photoRequest.Type}-{Guid.NewGuid()}";

                        return cloudinaryServices.UploadBookingInspectionImageAsync(
                            fileName,
                            file.OpenReadStream(),
                            cancellationToken
                        );
                    })
                    .ToList();

                var uploadedUrls = await Task.WhenAll(uploadTasks);

                photoResponses.Add(new PhotoResponse(photoRequest.Type, uploadedUrls));

                allInspectionPhotos.AddRange(
                    uploadedUrls.Select(url => new InspectionPhoto
                    {
                        InspectionId = existingInspection.Id,
                        Type = photoRequest.Type,
                        PhotoUrl = url,
                        Description = photoRequest.Note
                    })
                );
            }

            await context.InspectionPhotos.AddRangeAsync(allInspectionPhotos, cancellationToken);

            // Check if all required photos are uploaded and mark inspection as complete
            existingInspection.IsComplete = true;

            // After successfully processing photos and saving inspection
            if (existingInspection.IsComplete)
            {
                // Automatically confirm car return
                booking.IsCarReturned = true;
                booking.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(new Response(existingInspection.Id, photoResponses));
        }

        private static bool HasRequiredPhotoTypes(IEnumerable<InspectionPhotoType> providedTypes)
        {
            var requiredTypes = new[]
            {
                InspectionPhotoType.FuelGaugeFinal,
                InspectionPhotoType.Cleanliness
            };

            return requiredTypes.All(rt => providedTypes.Contains(rt));
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BookingId).NotEmpty().WithMessage("Yêu cầu mã đặt xe");
        }
    }
}
