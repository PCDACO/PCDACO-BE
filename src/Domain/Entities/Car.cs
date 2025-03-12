using System.ComponentModel.DataAnnotations.Schema;

using Domain.Shared;
using Domain.Enums;


namespace Domain.Entities;

public class Car : BaseEntity
{
    // Properties
    public required Guid OwnerId { get; set; }
    public required Guid ModelId { get; set; }
    public required Guid EncryptionKeyId { get; set; }
    public required Guid FuelTypeId { get; set; }
    public required Guid TransmissionTypeId { get; set; }
    public CarStatusEnum Status { get; set; } = CarStatusEnum.Pending;
    public required string EncryptedLicensePlate { get; set; }
    public required string Color { get; set; }
    public required int Seat { get; set; }
    public string Description { get; set; } = string.Empty;
    public required decimal FuelConsumption { get; set; }
    public bool RequiresCollateral { get; set; } = false;
    public required decimal Price { get; set; }
    public string Terms { get; set; } = string.Empty;
    public int TotalRented { get; set; } = 0;
    public decimal TotalEarning { get; set; } = 0;

    // Navigation Properties
    [ForeignKey(nameof(OwnerId))]
    public User Owner { get; set; } = null!;

    [ForeignKey(nameof(ModelId))]
    public Model Model { get; set; } = null!;

    [ForeignKey(nameof(EncryptionKeyId))]
    public EncryptionKey EncryptionKey { get; set; } = null!;

    [ForeignKey(nameof(FuelTypeId))]
    public FuelType FuelType { get; set; } = null!;

    [ForeignKey(nameof(TransmissionTypeId))]
    public TransmissionType TransmissionType { get; set; } = null!;

    public CarStatistic CarStatistic { get; set; } = null!;
    public CarGPS GPS { get; set; } = null!;
    public CarContract Contract { get; set; } = null!;
    public ICollection<CarAmenity> CarAmenities { get; set; } = [];
    public ICollection<ImageCar> ImageCars { get; set; } = [];
    public ICollection<CarReport> CarReports { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<InspectionSchedule> InspectionSchedules { get; set; } = [];
}