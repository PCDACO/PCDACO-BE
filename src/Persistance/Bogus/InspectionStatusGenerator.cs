using Domain.Constants;
using Domain.Entities;

namespace Persistance.Bogus;

public class InspectionStatusGenerator
{
    private static readonly string[] _inspectionStatus =
    [
        InspectionStatusNames.Pending,
        InspectionStatusNames.Scheduled,
        InspectionStatusNames.InProgress,
        InspectionStatusNames.Approved,
        InspectionStatusNames.Rejected,
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
