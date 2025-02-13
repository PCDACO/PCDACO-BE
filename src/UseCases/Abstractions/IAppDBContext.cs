using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace UseCases.Abstractions;

public interface IAppDBContext : IDisposable
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
    DbSet<Amenity> Amenities { get; }
    DbSet<BankAccount> BankAccounts { get; }
    DbSet<BankInfo> BankInfos { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<BookingStatus> BookingStatuses { get; }
    DbSet<Car> Cars { get; }
    DbSet<CarAmenity> CarAmenities { get; }
    DbSet<CarReport> CarReports { get; }
    DbSet<CarStatistic> CarStatistics { get; }
    DbSet<CarStatus> CarStatuses { get; }
    DbSet<Compensation> Compensations { get; }
    DbSet<CompensationStatus> CompensationStatuses { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<ContractStatus> ContractStatuses { get; }
    DbSet<License> Licenses { get; }
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
    DbSet<TransactionStatus> TransactionStatuses { get; }
    DbSet<TransactionType> TransactionTypes { get; }
    DbSet<TripTracking> TripTrackings { get; }
    DbSet<User> Users { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<UserStatistic> UserStatistics { get; }
    DbSet<WithdrawalRequest> WithdrawalRequests { get; }
    DbSet<WithdrawalRequestStatus> WithdrawalRequestStatuses { get; }
    DbSet<Model> Models { get; }
    DbSet<InspectionSchedule> InspectionSchedules { get; }
    DbSet<InspectionStatus> InspectionStatuses { get; }
}
