using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace UseCases.Abstractions;

public interface IAppDBContext : IDisposable
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
    DbSet<Amenity> Amenities { get; }
    DbSet<BankAccount> BankAccounts { get; }
    DbSet<BankInfo> BankInfos { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<Car> Cars { get; }
    DbSet<CarAmenity> CarAmenities { get; }
    DbSet<BookingReport> BookingReports { get; }
    DbSet<CarStatistic> CarStatistics { get; }
    DbSet<Compensation> Compensations { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<EncryptionKey> EncryptionKeys { get; }
    DbSet<Feedback> Feedbacks { get; }
    DbSet<FuelType> FuelTypes { get; }
    DbSet<ImageCar> ImageCars { get; }
    DbSet<ImageFeedback> ImageFeedbacks { get; }
    DbSet<ImageReport> ImageReports { get; }
    DbSet<ImageType> ImageTypes { get; }
    DbSet<Manufacturer> Manufacturers { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<TransmissionType> TransmissionTypes { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<TransactionType> TransactionTypes { get; }
    DbSet<TripTracking> TripTrackings { get; }
    DbSet<User> Users { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<UserStatistic> UserStatistics { get; }
    DbSet<WithdrawalRequest> WithdrawalRequests { get; }
    DbSet<Model> Models { get; }
    DbSet<InspectionSchedule> InspectionSchedules { get; }
    DbSet<CarContract> CarContracts { get; }
    DbSet<CarGPS> CarGPSes { get; }
    DbSet<GPSDevice> GPSDevices { get; }
    DbSet<CarInspection> CarInspections { get; }
    DbSet<InspectionPhoto> InspectionPhotos { get; }
}
