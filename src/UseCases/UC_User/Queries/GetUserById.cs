using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.UC_User.Queries;

public class GetUserById
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        // Basic user information
        Guid Id,
        Guid RoleId,
        string AvatarUrl,
        string Name,
        string Email,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone,
        decimal Balance,
        LicenseInfo? LicenseInfo,
        bool IsBanned,
        string BannedReason,
        string Role,
        // Related collections
        IEnumerable<CarResponse> Cars,
        IEnumerable<BookingResponse> Bookings,
        IEnumerable<ReportResponse> Reports
    )
    {
        public static async Task<Response> FromEntity(
            User user,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            // Decrypt sensitive information
            string decryptedKey = keyManagementService.DecryptKey(
                user.EncryptionKey.EncryptedKey,
                masterKey
            );

            string decryptedPhone = await aesEncryptionService.Decrypt(
                user.Phone,
                decryptedKey,
                user.EncryptionKey.IV
            );

            string decryptedLicenseNumber = string.Empty;
            if (!string.IsNullOrEmpty(user.EncryptedLicenseNumber))
            {
                decryptedLicenseNumber = await aesEncryptionService.Decrypt(
                    user.EncryptedLicenseNumber,
                    decryptedKey,
                    user.EncryptionKey.IV
                );
            }

            // Map cars, bookings and reports
            var cars = await Task.WhenAll(
                user.Cars?.Where(c => !c.IsDeleted)
                    .Select(async c => await CarResponse.FromEntity(c)) ?? []
            );

            var bookings =
                user.Bookings?.Where(b => !b.IsDeleted).Select(BookingResponse.FromEntity) ?? [];

            // For reports, we need to collect them from bookings related to user's cars
            var reports = new List<ReportResponse>();

            // Process reports directed to this user (as car owner or driver)
            if (user.Cars != null)
            {
                // Process reports where this user is the car owner
                var ownerReports = user
                    .Cars.Where(c => !c.IsDeleted)
                    .SelectMany(c => c.Bookings ?? Enumerable.Empty<Booking>())
                    .Where(b => !b.IsDeleted)
                    .SelectMany(b => b.BookingReports ?? Enumerable.Empty<BookingReport>())
                    .Where(r =>
                        !r.IsDeleted && r.ReportedById != user.Id && r.ReportedBy.IsDriver()
                    )
                    .Select(ReportResponse.FromEntity);

                reports.AddRange(ownerReports);
            }

            // Process reports where this user is the driver
            var driverReports =
                user.Bookings?.Where(b => !b.IsDeleted)
                    .SelectMany(b => b.BookingReports ?? Enumerable.Empty<BookingReport>())
                    .Where(r => !r.IsDeleted && r.ReportedById != user.Id && r.ReportedBy.IsOwner())
                    .Select(ReportResponse.FromEntity) ?? Enumerable.Empty<ReportResponse>();

            reports.AddRange(driverReports);

            return new(
                // Basic user information
                user.Id,
                user.RoleId,
                user.AvatarUrl,
                user.Name,
                user.Email,
                user.Address,
                user.DateOfBirth,
                decryptedPhone,
                user.Balance,
                string.IsNullOrEmpty(decryptedLicenseNumber)
                    ? null
                    : new LicenseInfo(
                        decryptedLicenseNumber,
                        user.LicenseImageFrontUrl,
                        user.LicenseImageBackUrl,
                        user.LicenseExpiryDate,
                        user.LicenseIsApproved,
                        user.LicenseRejectReason,
                        user.LicenseImageUploadedAt,
                        user.LicenseApprovedAt
                    ),
                user.IsBanned,
                user.BannedReason,
                user.Role.Name,
                // Related collections
                cars,
                bookings,
                reports
            );
        }
    }

    public record LicenseInfo(
        string LicenseNumber,
        string LicenseImageFrontUrl,
        string LicenseImageBackUrl,
        DateTimeOffset? LicenseExpiryDate,
        bool? LicenseIsApproved,
        string? LicenseRejectReason,
        DateTimeOffset? LicenseImageUploadedAt,
        DateTimeOffset? LicenseApprovedAt
    );

    public record CarResponse(
        Guid Id,
        Guid OwnerId,
        Guid ModelId,
        Guid FuelTypeId,
        Guid TransmissionTypeId,
        string Status,
        string LicensePlate,
        string Color,
        int Seat,
        string Description,
        decimal FuelConsumption,
        bool RequiresCollateral,
        decimal Price,
        string Terms,
        PickupLocationDetail PickupLocation,
        string ModelName,
        string ManufacturerName,
        string FuelTypeName,
        string TransmissionTypeName,
        IEnumerable<string> ImageUrls
    )
    {
        public static async Task<CarResponse> FromEntity(Car car)
        {
            return new(
                car.Id,
                car.OwnerId,
                car.ModelId,
                car.FuelTypeId,
                car.TransmissionTypeId,
                car.Status.ToString(),
                car.LicensePlate,
                car.Color,
                car.Seat,
                car.Description,
                car.FuelConsumption,
                car.RequiresCollateral,
                car.Price,
                car.Terms,
                new PickupLocationDetail(
                    car.PickupLocation.X,
                    car.PickupLocation.Y,
                    car.PickupAddress
                ),
                car.Model?.Name ?? string.Empty,
                car.Model?.Manufacturer?.Name ?? string.Empty,
                car.FuelType?.Name ?? string.Empty,
                car.TransmissionType?.Name ?? string.Empty,
                car.ImageCars?.Select(ic => ic.Url) ?? Enumerable.Empty<string>()
            );
        }
    }

    public record PickupLocationDetail(double Longitude, double Latitude, string Address);

    public record BookingResponse(
        Guid Id,
        Guid UserId,
        Guid CarId,
        string Status,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
        DateTimeOffset ActualReturnTime,
        decimal BasePrice,
        decimal PlatformFee,
        decimal ExcessDay,
        decimal ExcessDayFee,
        decimal TotalAmount,
        decimal TotalDistance,
        string Note,
        bool IsCarReturned,
        long? PayOSOrderCode,
        bool IsPaid,
        bool IsRefund,
        decimal? RefundAmount,
        DateTimeOffset? RefundDate,
        string CarModelName,
        string DriverName
    )
    {
        public static BookingResponse FromEntity(Booking booking) =>
            new(
                booking.Id,
                booking.UserId,
                booking.CarId,
                booking.Status.ToString(),
                booking.StartTime,
                booking.EndTime,
                booking.ActualReturnTime,
                booking.BasePrice,
                booking.PlatformFee,
                booking.ExcessDay,
                booking.ExcessDayFee,
                booking.TotalAmount,
                booking.TotalDistance,
                booking.Note,
                booking.IsCarReturned,
                booking.PayOSOrderCode,
                booking.IsPaid,
                booking.IsRefund,
                booking.RefundAmount,
                booking.RefundDate,
                booking.Car?.Model?.Name ?? string.Empty,
                booking.User?.Name ?? string.Empty
            );
    }

    public record ReportResponse(
        Guid Id,
        Guid BookingId,
        Guid ReportedById,
        string Title,
        string ReportType,
        string Description,
        string Status,
        Guid? CompensationPaidUserId,
        string? CompensationReason,
        decimal? CompensationAmount,
        bool? IsCompensationPaid,
        string? CompensationPaidImageUrl,
        DateTimeOffset? CompensationPaidAt,
        DateTimeOffset? ResolvedAt,
        Guid? ResolvedById,
        string? ResolutionComments,
        string ReporterName,
        string? ResolverName,
        IEnumerable<string> ImageUrls
    )
    {
        public static ReportResponse FromEntity(BookingReport report) =>
            new(
                report.Id,
                report.BookingId,
                report.ReportedById,
                report.Title,
                report.ReportType.ToString(),
                report.Description,
                report.Status.ToString(),
                report.CompensationPaidUserId,
                report.CompensationReason,
                report.CompensationAmount,
                report.IsCompensationPaid,
                report.CompensationPaidImageUrl,
                report.CompensationPaidAt,
                report.ResolvedAt,
                report.ResolvedById,
                report.ResolutionComments,
                report.ReportedBy?.Name ?? string.Empty,
                report.ResolvedBy?.Name,
                report.ImageReports?.Select(ir => ir.Url) ?? Enumerable.Empty<string>()
            );
    }

    internal sealed class Handler(
        IAppDBContext context,
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
            var user = await context
                .Users.AsNoTracking()
                .AsSplitQuery()
                // Include basic user related entities
                .Include(u => u.Role)
                .Include(u => u.EncryptionKey)
                // Include user's cars with related data
                .Include(u => u.Cars)
                .ThenInclude(c => c.Model)
                .ThenInclude(m => m.Manufacturer)
                .Include(u => u.Cars)
                .ThenInclude(c => c.FuelType)
                .Include(u => u.Cars)
                .ThenInclude(c => c.TransmissionType)
                .Include(u => u.Cars)
                .ThenInclude(c => c.ImageCars)
                // Include bookings related to user's cars with their reports
                .Include(u => u.Cars)
                .ThenInclude(c => c.Bookings)
                .ThenInclude(b => b.BookingReports)
                .ThenInclude(r => r.ReportedBy)
                .ThenInclude(r => r.Role)
                .Include(u => u.Cars)
                .ThenInclude(c => c.Bookings)
                .ThenInclude(b => b.BookingReports)
                .ThenInclude(r => r.ResolvedBy)
                .Include(u => u.Cars)
                .ThenInclude(c => c.Bookings)
                .ThenInclude(b => b.BookingReports)
                .ThenInclude(r => r.ImageReports)
                // Include user bookings with their reports
                .Include(u => u.Bookings)
                .ThenInclude(b => b.Car)
                .ThenInclude(c => c.Model)
                .Include(u => u.Bookings)
                .ThenInclude(b => b.BookingReports)
                .ThenInclude(r => r.ReportedBy)
                .ThenInclude(r => r.Role)
                .FirstOrDefaultAsync(u => u.Id == request.Id && !u.IsDeleted, cancellationToken);

            if (user is null)
                return Result.NotFound(ResponseMessages.UserNotFound);

            var response = await Response.FromEntity(
                user,
                encryptionSettings.Key,
                aesEncryptionService,
                keyManagementService
            );

            return Result.Success(response, ResponseMessages.Fetched);
        }
    }
}
