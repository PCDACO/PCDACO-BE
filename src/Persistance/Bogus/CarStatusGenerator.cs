using Domain.Entities;

namespace Persistance.Bogus;

public class CarStatusGenerator
{
    private static readonly string[] _carStatus = ["Available", "Rented", "Inactive"];
    public static CarStatus[] Execute()
    {
        return [.. _carStatus.Select(status => {
            return new CarStatus()
            {
                Name = status,
            };
        })];
    }
}