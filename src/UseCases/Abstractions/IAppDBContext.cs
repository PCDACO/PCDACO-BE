using Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace UseCases.Abstractions;

public interface IAppDBContext : IDisposable
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
    DbSet<Amenity> Amenities { get; }
    DbSet<BankAccount> BankAccounts { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<Car> Cars { get; }
    DbSet<CarAmenity> CarAmenities { get; }
    DbSet<CarReport> CarReports { get; }
    DbSet<CarStatistic> CarStatistics { get; }
    DbSet<Driver> Drivers { get; }
    DbSet<EncryptionKey> EncryptionKeys { get; }
    DbSet<Feedback> Feedbacks { get; }
    DbSet<FinancialReport> FinancialReports { get; }
    DbSet<ImageCar> ImageCars { get; }
    DbSet<ImageFeedback> ImageFeedbacks { get; }
    DbSet<ImageReport> ImageReports { get; }
    DbSet<Manufacturer> Manufacturers { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<TripTracking> TripTrackings { get; }
    DbSet<User> Users { get; }
    DbSet<UserStatistic> UserStatistics { get; }
    DbSet<Withdrawal> Withdrawals { get; }
}