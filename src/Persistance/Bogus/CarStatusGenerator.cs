using Domain.Constants;
using Domain.Entities;

namespace Persistance.Bogus;

public class CarStatusGenerator
{
    private static readonly string[] _carStatus =
    [
        CarStatusNames.Available,
        CarStatusNames.Rented,
        CarStatusNames.Inactive,
        CarStatusNames.Pending,
    ];

    public static CarStatus[] Execute()
    {
        return
        [
            .. _carStatus.Select(status =>
            {
                return new CarStatus() { Name = status };
            }),
        ];
    }
}
