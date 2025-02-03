using Domain.Entities;
using UUIDNext;

namespace Persistance.Bogus;

public class ModelGenerator
{
    private static readonly Dictionary<string, string[]> CarModels = new()
    {
        {
            "Toyota",
            ["Camry", "Corolla", "RAV4", "Highlander", "Land Cruiser", "Fortuner", "Innova"]
        },
        { "Honda", ["Civic", "Accord", "CR-V", "HR-V", "City", "Jazz", "Pilot"] },
        { "Ford", ["Mustang", "F-150", "Explorer", "Escape", "Ranger", "Focus", "Everest"] },
        {
            "Chevrolet",
            ["Silverado", "Suburban", "Tahoe", "Camaro", "Corvette", "Malibu", "Equinox"]
        },
        { "Mercedes-Benz", ["C-Class", "E-Class", "S-Class", "GLC", "GLE", "GLA", "AMG GT"] },
        { "BMW", ["3 Series", "5 Series", "7 Series", "X3", "X5", "M3", "M5"] },
        { "Audi", ["A4", "A6", "A8", "Q5", "Q7", "RS6", "R8"] },
        { "Volkswagen", ["Golf", "Passat", "Tiguan", "Atlas", "Jetta", "Arteon", "ID.4"] },
        { "Hyundai", ["Elantra", "Sonata", "Tucson", "Santa Fe", "Palisade", "Kona", "Venue"] },
        { "Kia", ["Forte", "K5", "Sportage", "Telluride", "Sorento", "Carnival", "EV6"] },
        { "Mazda", ["Mazda3", "Mazda6", "CX-5", "CX-9", "MX-5", "CX-30", "CX-50"] },
        { "Nissan", ["Altima", "Maxima", "Rogue", "Pathfinder", "GT-R", "Z", "Ariya"] },
        { "Subaru", ["Impreza", "Legacy", "Outback", "Forester", "Crosstrek", "WRX", "BRZ"] },
        { "Tesla", ["Model 3", "Model S", "Model X", "Model Y", "Cybertruck"] },
        { "Volvo", ["S60", "S90", "XC40", "XC60", "XC90", "C40", "V60"] },
        { "Porsche", ["911", "Cayenne", "Panamera", "Macan", "Taycan", "718 Cayman"] },
        { "Jaguar", ["F-TYPE", "XF", "XE", "F-PACE", "E-PACE", "I-PACE", "XJ"] },
        { "Lexus", ["ES", "IS", "LS", "RX", "NX", "GX", "LX"] },
        { "Land Rover", ["Range Rover", "Discovery", "Defender", "Velar", "Evoque", "Sport"] },
        { "Ferrari", ["F8 Tributo", "SF90 Stradale", "812 Superfast", "Roma", "Portofino"] },
        { "Lamborghini", ["Huracán", "Aventador", "Urus", "Revuelto", "Gallardo"] },
        { "Bugatti", ["Chiron", "Veyron", "Divo", "Centodieci", "Mistral"] },
        { "McLaren", ["720S", "765LT", "Artura", "GT", "P1", "Senna", "Speedtail"] },
        { "Rolls-Royce", ["Phantom", "Ghost", "Cullinan", "Wraith", "Dawn", "Spectre"] },
        { "Bentley", ["Continental GT", "Flying Spur", "Bentayga", "Mulsanne", "Bacalar"] },
        { "Peugeot", ["208", "2008", "3008", "5008", "508", "e-208", "e-2008"] },
        { "Renault", ["Clio", "Captur", "Megane", "Arkana", "Austral", "Espace", "Scenic"] },
        { "Citroën", ["C3", "C4", "C5 X", "ë-C4", "C5 Aircross", "Berlingo", "SpaceTourer"] },
        { "Fiat", ["500", "Panda", "Tipo", "500X", "500L", "124 Spider", "Ducato"] },
        { "Jeep", ["Wrangler", "Grand Cherokee", "Cherokee", "Compass", "Renegade", "Gladiator"] },
    };

    public static Model[] Execute(Manufacturer[] manufacturers)
    {
        var models = new List<Model>();
        var random = new Random();

        foreach (var manufacturer in manufacturers)
        {
            if (CarModels.TryGetValue(manufacturer.Name, out var brandModels))
            {
                foreach (var modelName in brandModels)
                {
                    bool isRandom = random.Next(0, 2) == 1;
                    bool isRandomUpdate = random.Next(0, 2) == 1;

                    models.Add(
                        new Model
                        {
                            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
                            ManufacturerId = manufacturer.Id,
                            Name = modelName,
                            ReleaseDate = DateTimeOffset.UtcNow.AddYears(-random.Next(0, 10)),
                            UpdatedAt = isRandomUpdate ? DateTime.UtcNow : null,
                            IsDeleted = isRandom,
                            DeletedAt = isRandom ? DateTime.UtcNow : null,
                        }
                    );
                }
            }
        }

        return [.. models];
    }
}
