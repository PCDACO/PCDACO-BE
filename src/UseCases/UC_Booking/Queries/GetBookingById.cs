using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Booking.Queries;

public sealed class GetBookingById
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid Id,
        CarDetail Car,
        UserDetail Driver,
        UserDetail Owner,
        BookingDetail Booking,
        PaymentDetail Payment,
        TripDetail Trip,
        FeedbackDetail[] Feedbacks
    )
    {
        public static async Task<Response> FromEntity(
            Booking booking,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            string decryptedKey = keyManagementService.DecryptKey(
                booking.Car.EncryptionKey.EncryptedKey,
                masterKey
            );

            string decryptedLicensePlate = await aesEncryptionService.Decrypt(
                booking.Car.EncryptedLicensePlate,
                decryptedKey,
                booking.Car.EncryptionKey.IV
            );

            return new(
                booking.Id,
                new CarDetail(
                    booking.Car.Id,
                    booking.Car.Model.Name,
                    decryptedLicensePlate,
                    booking.Car.Color,
                    booking.Car.Seat,
                    booking.Car.TransmissionType.Name,
                    booking.Car.FuelType.Name
                ),
                new UserDetail(
                    booking.User.Id,
                    booking.User.Name,
                    booking.User.Phone,
                    booking.User.Email
                ),
                new UserDetail(
                    booking.Car.Owner.Id,
                    booking.Car.Owner.Name,
                    booking.Car.Owner.Phone,
                    booking.Car.Owner.Email
                ),
                new BookingDetail(
                    booking.StartTime,
                    booking.EndTime,
                    booking.ActualReturnTime,
                    booking.TotalDistance,
                    booking.Status.Name,
                    booking.Note
                ),
                new PaymentDetail(
                    booking.BasePrice,
                    booking.PlatformFee,
                    booking.ExcessDay,
                    booking.ExcessDayFee,
                    booking.TotalAmount,
                    booking.IsPaid
                ),
                new TripDetail(
                    booking
                        .TripTrackings.OrderByDescending(t => t.Id)
                        .FirstOrDefault()
                        ?.CumulativeDistance ?? 0
                ),
                [
                    .. booking.Feedbacks.Select(f => new FeedbackDetail(
                        f.Id,
                        f.Point,
                        f.Content,
                        f.Type,
                        f.User.Name
                    ))
                ]
            );
        }
    }

    // Add record types for each detail section
    public record CarDetail(
        Guid Id,
        string ModelName,
        string LicensePlate,
        string Color,
        int Seat,
        string TransmissionType,
        string FuelType
    );

    public record UserDetail(Guid Id, string Name, string Phone, string Email);

    public record BookingDetail(
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
        DateTimeOffset ActualReturnTime,
        decimal TotalDistance,
        string Status,
        string Note
    );

    public record PaymentDetail(
        decimal BasePrice,
        decimal PlatformFee,
        decimal ExcessDay,
        decimal ExcessDayFee,
        decimal TotalAmount,
        bool IsPaid
    );

    public record TripDetail(decimal TotalDistance);

    public record FeedbackDetail(
        Guid Id,
        int Rating,
        string Content,
        FeedbackTypeEnum Type,
        string UserName
    );

    internal sealed class Handler(
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
            var booking = await context
                .Bookings.Include(b => b.Status)
                .Include(b => b.Car) .ThenInclude(c => c.Model)
                .Include(b => b.Car) .ThenInclude(c => c.TransmissionType)
                .Include(b => b.Car) .ThenInclude(c => c.FuelType)
                .Include(b => b.Car) .ThenInclude(c => c.EncryptionKey)
                .Include(b => b.Car) .ThenInclude(c => c.Owner)
                .Include(b => b.User)
                .Include(b => b.TripTrackings)
                .Include(b => b.Feedbacks) .ThenInclude(f => f.User)
                .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            // Validate access rights
            if (
                !currentUser.User!.IsAdmin() // Admin can view any booking
                && booking.UserId != currentUser.User.Id // Driver can only view their own booking
                && booking.Car.OwnerId != currentUser.User.Id // Owner can only view their own car's booking
            )
                return Result.Forbidden("Bạn không có quyền xem thông tin này");

            return Result.Success(
                await Response.FromEntity(
                    booking,
                    encryptionSettings.Key,
                    aesEncryptionService,
                    keyManagementService
                )
            );
        }
    }
}
