using Domain.Data;
using Domain.Entities;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using Persistance.Bogus;
using Persistance.Data;
using UseCases.Abstractions;
using UseCases.Utils;

namespace API.Utils;

public class UpdateDatabase
{
    public static async Task Execute(IApplicationBuilder app)
    {
        // Update database
        using var scope = app
            .ApplicationServices.GetRequiredService<IServiceScopeFactory>()
            .CreateScope();
        using var context = scope.ServiceProvider.GetService<AppDBContext>();
        var encryptionSettings = scope.ServiceProvider.GetRequiredService<EncryptionSettings>();
        var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
        var aesEncryptionService =
            scope.ServiceProvider.GetRequiredService<IAesEncryptionService>();
        var keyManageService = scope.ServiceProvider.GetRequiredService<IKeyManagementService>();

        // Create geometry factory for spatial data
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        if (context is null)
            throw new ArgumentNullException(nameof(context));
        //context.Database.EnsureDeleted();
        context.Database.Migrate();
        // Seed data
        Amenity[] amenities = AmenityGenerator.Execute();
        BankInfo[] bankInfos = BankInfoGenerator.Execute();
        FuelType[] fuelTypes = FuelTypeGenerator.Execute();
        ImageType[] imageTypes = ImageTypeGenerator.Execute();
        Manufacturer[] manufacturers = ManufacturerGenerator.Execute();
        TransactionType[] transactionTypes = TransactionTypeGenerator.Execute();
        TransmissionType[] transmissionTypes = TransmissionTypeGenerator.Execute();
        UserRole[] userRoles = UserRoleGenerator.Execute();
        Model[] models = ModelGenerator.Execute(manufacturers);
        GPSDevice[] gpsDevices = GPSDeviceGenerator.Execute();
        User[] users = await UserGenerator.Execute(
            encryptionSettings,
            aesEncryptionService,
            keyManageService,
            tokenService
        );
        Car[] cars = await CarGenerator.Execute(
            transmissionTypes,
            models,
            fuelTypes,
            encryptionSettings,
            aesEncryptionService,
            keyManageService,
            tokenService,
            geometryFactory
        );
        // Generate inspection schedules
        InspectionSchedule[] inspectionSchedules = InspectionScheduleGenerator.Execute(
            cars,
            users,
            userRoles
        );
        List<Task> tasks = [];
        if (!await context.UserRoles.AnyAsync())
            tasks.Add(context.AddRangeAsync(userRoles));
        if (!await context.Amenities.AnyAsync())
            tasks.Add(context.AddRangeAsync(amenities));
        if (!await context.BankInfos.AnyAsync())
            tasks.Add(context.AddRangeAsync(bankInfos));
        if (!await context.FuelTypes.AnyAsync())
            tasks.Add(context.AddRangeAsync(fuelTypes));
        if (!await context.ImageTypes.AnyAsync())
            tasks.Add(context.AddRangeAsync(imageTypes));
        if (!await context.TransactionTypes.AnyAsync())
            tasks.Add(context.AddRangeAsync(transactionTypes));
        if (!await context.TransmissionTypes.AnyAsync())
            tasks.Add(context.AddRangeAsync(transmissionTypes));
        if (!await context.Manufacturers.AnyAsync())
            tasks.Add(context.AddRangeAsync(manufacturers));
        if (!await context.Models.AnyAsync())
            tasks.Add(context.AddRangeAsync(models));
        if (!await context.Users.AnyAsync())
            tasks.Add(context.AddRangeAsync(users));
        if (!await context.Cars.AnyAsync())
            tasks.Add(context.AddRangeAsync(cars));
        if (!await context.InspectionSchedules.AnyAsync())
            tasks.Add(context.AddRangeAsync(inspectionSchedules));
        if (!await context.GPSDevices.AnyAsync())
            tasks.Add(context.AddRangeAsync(gpsDevices));
        await Task.WhenAll(tasks);
        await context.SaveChangesAsync();
        // Load init data to initial objects.
        UserRolesData userRolesData = app.ApplicationServices.GetRequiredService<UserRolesData>();
        userRolesData.Set(userRoles);
    }
}
