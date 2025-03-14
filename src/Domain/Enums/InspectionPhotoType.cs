namespace Domain.Enums;

public enum InspectionPhotoType
{
    ExteriorFront,
    ExteriorBack,
    ExteriorLeft,
    ExteriorRight,
    InteriorFront,
    InteriorBack,
    FuelGauge,
    Odometer,
    CarKeys,
    TrunkEmpty,
    Damages, // Optional, multiple allowed
    Other // Optional, multiple allowed
}
