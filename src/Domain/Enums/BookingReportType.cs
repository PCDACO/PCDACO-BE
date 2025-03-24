namespace Domain.Enums;

public enum BookingReportType
{
    Conflict, // Conflict/dispute between Owner and Driver (e.g. disagreement over car condition or collateral return)
    Accident, // Report of an accident (typically reported by the Driver during a trip)
    FineNotice, // Report indicating that the Owner has received a traffic fine notice ("giấy báo phạt nguội") attributable to the Driver
    Damage, // Report regarding additional or disputed car damage after rental
    Maintenance, // Report regarding car maintenance issues (e.g. not properly maintained before rental)
    Other // Any other case not covered above
}
