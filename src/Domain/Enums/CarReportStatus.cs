namespace Domain.Enums;

public enum CarReportStatus
{
    Pending, // Initial status when report is created
    UnderReview, // Report is being reviewed by staff
    Resolved, // Report has been resolved
    Rejected // Report has been rejected
}
