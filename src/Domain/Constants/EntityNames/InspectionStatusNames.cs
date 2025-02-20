namespace Domain.Constants.EntityNames;

public class InspectionStatusNames
{
    public const string Pending = "Pending"; // Initial state when schedule created
    public const string Scheduled = "Scheduled"; // Time agreed with owner
    public const string InProgress = "InProgress"; // During inspection
    public const string Approved = "Approved"; // Car passed inspection
    public const string Rejected = "Rejected"; // Failed inspection
}
