using Domain.Entities;

namespace Persistance.Bogus;

public class InspectionStatusGenerator
{
    private static readonly string[] _inspectionStatus =
    [
        "Pending", // Initial state when schedule created
        "Scheduled", // Time agreed with owner
        "InProgress", // During inspection
        "Approved", // Car passed inspection
        "Rejected", // Failed inspection
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
