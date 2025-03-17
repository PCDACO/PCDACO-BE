using System.Linq.Expressions;

using Domain.Entities;
using Domain.Shared;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;

namespace Persistance.Data;

public class AppDBContext(DbContextOptions context) : DbContext(context), IAppDBContext
{
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankInfo> BankInfos => Set<BankInfo>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<CarAmenity> CarAmenities => Set<CarAmenity>();
    public DbSet<CarReport> CarReports => Set<CarReport>();
    public DbSet<CarStatistic> CarStatistics => Set<CarStatistic>();
    public DbSet<EncryptionKey> EncryptionKeys => Set<EncryptionKey>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<ImageCar> ImageCars => Set<ImageCar>();
    public DbSet<ImageFeedback> ImageFeedbacks => Set<ImageFeedback>();
    public DbSet<ImageReport> ImageReports => Set<ImageReport>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TripTracking> TripTrackings => Set<TripTracking>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserStatistic> UserStatistics => Set<UserStatistic>();
    public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();
    public DbSet<Compensation> Compensations => Set<Compensation>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<FuelType> FuelTypes => Set<FuelType>();
    public DbSet<ImageType> ImageTypes => Set<ImageType>();
    public DbSet<TransactionType> TransactionTypes => Set<TransactionType>();
    public DbSet<TransmissionType> TransmissionTypes => Set<TransmissionType>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Model> Models => Set<Model>();
    public DbSet<InspectionSchedule> InspectionSchedules => Set<InspectionSchedule>();
    public DbSet<CarContract> CarContracts => Set<CarContract>();
    public DbSet<CarGPS> CarGPSes => Set<CarGPS>();
    public DbSet<GPSDevice> GPSDevices => Set<GPSDevice>();
    public DbSet<CarInspection> CarInspections => Set<CarInspection>();
    public DbSet<InspectionPhoto> InspectionPhotos => Set<InspectionPhoto>();

    async Task IAppDBContext.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Apply query filter for BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Equal(
                    Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
                    Expression.Constant(false)
                );
                modelBuilder
                    .Entity(entityType.ClrType)
                    .HasQueryFilter(Expression.Lambda(body, parameter));
            }
        }
    }
}
