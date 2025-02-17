using Domain.Entities;

namespace Persistance.Bogus;

public class CarStatusGenerator
{
    private static readonly string[] _carStatus = ["Available", "Rented", "Inactive","Pending",];
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