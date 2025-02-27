using Domain.Entities;

using UUIDNext;

namespace Persistance.Bogus;

public class ManufacturerGenerator
{
    private static readonly string[] CarBrands = [
    "Toyota",
    "Honda",
    "Ford",
    "Chevrolet",
    "Mercedes-Benz",
    "BMW",
    "Audi",
    "Volkswagen",
    "Hyundai",
    "Kia",
    "Mazda",
    "Nissan",
    "Subaru",
    "Tesla",
    "Volvo",
    "Porsche",
    "Jaguar",
    "Lexus",
    "Land Rover",
    "Ferrari",
    "Lamborghini",
    "Bugatti",
    "McLaren",
    "Rolls-Royce",
    "Bentley",
    "Peugeot",
    "Renault",
    "Citroën",
    "Fiat",
    "Jeep"
];

    public static Manufacturer[] Execute()
    {
        return [.. CarBrands.Select(brand =>
        {
            return new Manufacturer
            {
                Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                Name = brand,
            };
        })];
    }
}