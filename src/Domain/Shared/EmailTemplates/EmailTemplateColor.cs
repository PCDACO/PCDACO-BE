namespace Domain.Shared.EmailTemplates;

public static class EmailTemplateColors
{
    // Booking Created and Approve (Driver)
    public const string SuccessHeader = "#4CAF50";
    public const string SuccessBackground = "#e8f5e9";
    public const string SuccessAccent = "#2E7D32";

    // Booking Created (Owner)
    public const string NewBookingHeader = "#FF9800";
    public const string NewBookingBackground = "#fff3e0";
    public const string NewBookingAccent = "#E65100";

    // Booking Rejected and Cancelled
    public const string RejectedHeader = "#F44336";
    public const string RejectedBackground = "#ffebee";
    public const string RejectedAccent = "#C62828";

    // Complete Booking
    public const string CompleteHeader = "#4c9faf";
    public const string CompleteBackground = "#e8f3f5";
    public const string CompleteAccent = "#1976D2";

    // Warning colors for expired/timeout notifications
    public const string WarningHeader = "#FF9800"; // Orange
    public const string WarningBackground = "#fff3e0"; // Light Orange
    public const string WarningAccent = "#F57C00"; // Dark Orange

    public const string Warning = "#fff3e0";
    public const string Footer = "#666";
}
