using System.ComponentModel.DataAnnotations.Schema;

using Domain.Enums;
using Domain.Shared;

namespace Domain.Entities;

public class Car : BaseEntity
{
    // Properties
    public required Guid OwnerId { get; set; }
    public required Guid ManufacturerId { get; set; }
    public required Guid EncryptionKeyId { get; set; }
    public required string Name { get; set; }
    public required string LicensePlate { get; set; }
    public required string Color { get; set; }
    public required string Seat { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransmissionType TransmissionType { get; set; } = TransmissionType.Auto;
    public FuelType FuelType { get; set; } = FuelType.Electric;
    public required decimal FuelConsumption { get; set; }
    public bool RequiresCollateral { get; set; } = false;
    public CarStatus Status { get; set; } = CarStatus.Available;
    public required decimal PricePerHour { get; set; }
    public required decimal PricePerDay { get; set; }
    public decimal? Latitude { get; set; } = null!;
    public decimal? Longtitude { get; set; } = null!;
    public int TotalRented { get; set; } = 0;
    public decimal TotalEarning { get; set; } = 0;
    // Navigation Properties
    [ForeignKey(nameof(OwnerId))]
    public User Owner { get; set; } = null!;
    [ForeignKey(nameof(ManufacturerId))]
    public Manufacturer Manufacturer { get; set; } = null!;
    [ForeignKey(nameof(EncryptionKeyId))]
    public EncryptionKey EncryptionKey { get; set; } = null!;
    public CarStatistic CarStatistic { get; set; } = null!;
    public ICollection<CarAmenity> CarAmenities { get; set; } = [];
    public ICollection<ImageCar> ImageCars { get; set; } = [];
    public ICollection<CarReport> CarReports { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
}