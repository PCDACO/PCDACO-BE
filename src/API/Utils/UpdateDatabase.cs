using Domain.Entities;

using Microsoft.EntityFrameworkCore;

using Persistance.Bogus;
using Persistance.Data;
namespace API.Utils;

public class UpdateDatabase
{
    public static void Execute(IApplicationBuilder app)
    {
        // Update database
        using var scope = app.ApplicationServices
        .GetRequiredService<IServiceScopeFactory>()
        .CreateScope();
        using var context = scope.ServiceProvider.GetService<AppDBContext>();
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.Database.EnsureDeleted();
        context.Database.Migrate();
        // Seed data
        Amenity[] amenity = AmenityGenerator.Execute();
        Manufacturer[] manufacturers = ManufacturerGenerator.Execute();
        context.AddRange(amenity);
        context.AddRange(manufacturers);
        context.SaveChanges();

    }
}