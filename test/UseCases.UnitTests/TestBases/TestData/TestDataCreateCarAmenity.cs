using Domain.Entities;
using Persistance.Data;

namespace UseCases.UnitTests.TestBases.TestData;

public static class TestDataCreateCarAmenity
{
    private static CarAmenity CreateCar(Guid carId, Guid amenityId) =>
        new() { CarId = carId, AmenityId = amenityId };

    public static async Task<CarAmenity> CreateTestCarAmenity(
        AppDBContext dBContext,
        Guid carId,
        Guid amenityId
    )
    {
        var carAmenity = CreateCar(carId, amenityId);

        await dBContext.CarAmenities.AddAsync(carAmenity);
        await dBContext.SaveChangesAsync();

        return carAmenity;
    }
}
