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

public sealed class PreInspectionImages
{
    public sealed record PhotoRequest(
        InspectionPhotoType Type,
        IFormFile[] Files,
        string Note = ""
    );

    public sealed record Command : IRequest<Result<Response>>
    {
        public required Guid BookingId { get; init; }
        public required List<PhotoRequest> Photos { get; init; }
    }

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
                .Bookings.AsNoTracking()
                .Include(b => b.CarInspections.Where(i => i.Type == InspectionType.PreBooking))
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound(ResponseMessages.BookingNotFound);

            if (booking.Status != BookingStatusEnum.Approved)
                return Result.Error("Không thể tìm thấy đặt xe hoặc đặt xe không hợp lệ!");

            if (booking.StartTime > DateTime.Now.AddHours(24))
                return Result.Error(
                    "Chỉ có thể gửi hình ảnh kiểm tra trong vòng 24 giờ trước thời gian bắt đầu!"
                );

            var existingInspection = booking.CarInspections.FirstOrDefault(i =>
                i.BookingId == request.BookingId && i.Type == InspectionType.PreBooking
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
                    Type = InspectionType.PreBooking,
                    IsComplete = false
                };

                await context.CarInspections.AddAsync(existingInspection, cancellationToken);
            }

            // Process new photos
            var photoResponses = new List<PhotoResponse>();
            var allInspectionPhotos = new List<InspectionPhoto>();

            foreach (var photoRequest in request.Photos)
            {
                var uploadTasks = photoRequest
                    .Files.Select(file =>
                    {
                        var fileName =
                            $"PreInspection-{booking.Id}-{photoRequest.Type}-{Guid.NewGuid()}";

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

            // Check if all required photos are uploaded
            var inspection = await context.CarInspections.FindAsync(
                [existingInspection.Id],
                cancellationToken
            );

            inspection!.IsComplete = HasAllRequiredPhotos(request.Photos.Select(p => p.Type));

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(new Response(existingInspection.Id, photoResponses));
        }

        private static bool HasAllRequiredPhotos(IEnumerable<InspectionPhotoType> providedTypes)
        {
            var requiredTypes = new[]
            {
                InspectionPhotoType.ExteriorCar,
                InspectionPhotoType.FuelGauge,
                InspectionPhotoType.CarKey,
                InspectionPhotoType.TrunkSpace
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
