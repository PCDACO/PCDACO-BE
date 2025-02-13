using Domain.Entities;

namespace Persistance.Bogus;

public class InspectionStatusGenerator
{
    private static readonly string[] _inspectionStatus =
    [
        "Pending", // Initial state when inspection is scheduled
        "Completed", // Inspection has been done
        "Cancelled", // Inspection was cancelled
        "Failed", // Car failed inspection
    ];

    public static InspectionStatus[] Execute()
    {
        return
        [
            .. _inspectionStatus.Select(status =>
            {
                return new InspectionStatus() { Name = status };
            }),
        ];
    }
}
