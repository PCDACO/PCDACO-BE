using Domain.Constants.EntityNames;
using Domain.Entities;

namespace Persistance.Bogus;

public class FuelTypeGenerator
{
    private static readonly string[] _fuelTypes = [
        FuelTypeNames.Gasoline,
        FuelTypeNames.Diesel,
        FuelTypeNames.Electric,
        FuelTypeNames.Hybrid,
        ];
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