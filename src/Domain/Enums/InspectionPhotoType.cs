namespace Domain.Enums;

public enum InspectionPhotoType
{
    // Pre-booking inspection
    ExteriorCar, // Photo of the car exterior
    FuelGauge, // Photo of the fuel gauge
    ParkingLocation, // Photo of the parking location
    CarKey, // Photo of the car key
    TrunkSpace, // Photo of the trunk space

    // Post-booking inspection
    FuelGaugeFinal, // Photo of the final fuel gauge
    Scratches, // Photo of scratches
    Cleanliness, // Photo of the car cleanliness
    TollFees, // Photo of toll fees

    // Vehicle inspection certificate
    VehicleInspectionCertificate,

    // Other types
    Other, // Other types of inspection photos
}
