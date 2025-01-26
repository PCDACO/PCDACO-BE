using Domain.Entities;

namespace Persistance.Bogus;

public class FuelTypeGenerator
{
    private static readonly string[] _fuelTypes = ["Gasoline", "Diesel", "Electric", "Hybrid"];
    public static FuelType[] Execute()
    {
        return [.. _fuelTypes.Select(status => {
            return new FuelType()
            {
                Name = status,
            };
        })];
    }
}