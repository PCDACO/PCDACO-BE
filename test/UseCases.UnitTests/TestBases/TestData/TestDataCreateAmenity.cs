using Domain.Entities;
using Persistance.Data;
using UUIDNext;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateAmenity
{
    private static Amenity CreateAmenity(bool isDeleted = false) =>
        new()
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Name = "WiFi",
            Description = "High-speed internet",
            IconUrl = "http://test.com/test.jpg",
            IsDeleted = isDeleted,
        };

    public static async Task<Amenity> CreateTestAmenity(
        AppDBContext dBContext,
        bool isDeleted = false
    )
    {
        var amenity = CreateAmenity(isDeleted);

        await dBContext.Amenities.AddAsync(amenity);
        await dBContext.SaveChangesAsync();

        return amenity;
    }

    public static async Task<List<Amenity>> CreateTestAmenities(AppDBContext dBContext)
    {
        List<Amenity> amenityList =
        [
            new()
            {
                Name = "WiFi",
                Description = "High-speed internet",
                IconUrl = "http://test.com/test.jpg",
            },
            new()
            {
                Name = "Air Conditioning",
                Description = "Cooling system",
                IconUrl = "http://test.com/test2.jpg",
            },
            new()
            {
                Name = "Parking",
                Description = "Car parking space",
                IconUrl = "http://test.com/test3.jpg",
            },
        ];

        var amenities = new List<Amenity>();

        for (var i = 0; i < amenityList.Count; i++)
        {
            var amenity = CreateAmenity();
            amenity.Name = amenityList[i].Name;
            amenity.Description = amenityList[i].Description;

            amenities.Add(amenity);
        }

        await dBContext.Amenities.AddRangeAsync(amenities);
        await dBContext.SaveChangesAsync();

        return amenities;
    }
}
