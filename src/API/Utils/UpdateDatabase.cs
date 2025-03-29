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
        //get context scope
        using var scope = app
            .ApplicationServices.GetRequiredService<IServiceScopeFactory>()
            .CreateScope();
        using var context = scope.ServiceProvider.GetService<AppDBContext>();
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        //init needed objects
        //
        context.Database.EnsureDeleted();
        context.Database.Migrate();
        List<Task> tasks = [];
        UserRole[] userRoles = [];
        //check if there is no data in db then update db
        if (!(await context.ImageTypes.AnyAsync()))
        {
            var encryptionSettings = scope.ServiceProvider.GetRequiredService<EncryptionSettings>();
            var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
            var aesEncryptionService =
                scope.ServiceProvider.GetRequiredService<IAesEncryptionService>();
            var keyManageService =
                scope.ServiceProvider.GetRequiredService<IKeyManagementService>();
            // Create geometry factory for spatial data
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            // Seed data
            Amenity[] amenities = AmenityGenerator.Execute();
            BankInfo[] bankInfos = BankInfoGenerator.Execute();
            FuelType[] fuelTypes = FuelTypeGenerator.Execute();
            ImageType[] imageTypes = ImageTypeGenerator.Execute();
            Manufacturer[] manufacturers = ManufacturerGenerator.Execute();
            TransactionType[] transactionTypes = TransactionTypeGenerator.Execute();
            TransmissionType[] transmissionTypes = TransmissionTypeGenerator.Execute();
            userRoles = UserRoleGenerator.Execute();
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
            BankAccount[] bankAccounts = await BankAccountGenerator.Execute(
                users,
                bankInfos,
                encryptionSettings,
                aesEncryptionService,
                keyManageService
            );
            WithdrawalRequest[] withdrawalRequests = WithdrawalRequestGenerator.Execute(
                users,
                bankAccounts
            );
            Booking[] bookings = BookingGenerator.Execute(users, cars, userRoles, 20);
            Transaction[] transactions = TransactionGenerator.Execute(
                users,
                transactionTypes,
                bankAccounts
            );
            BookingReport[] reports = BookingReportGenerator.Execute(bookings, users, cars);
            tasks.Add(context.AddRangeAsync(userRoles));
            tasks.Add(context.AddRangeAsync(amenities));
            tasks.Add(context.AddRangeAsync(bankInfos));
            tasks.Add(context.AddRangeAsync(fuelTypes));
            tasks.Add(context.AddRangeAsync(imageTypes));
            tasks.Add(context.AddRangeAsync(transactionTypes));
            tasks.Add(context.AddRangeAsync(transmissionTypes));
            tasks.Add(context.AddRangeAsync(manufacturers));
            tasks.Add(context.AddRangeAsync(models));
            tasks.Add(context.AddRangeAsync(users));
            tasks.Add(context.AddRangeAsync(cars));
            tasks.Add(context.AddRangeAsync(inspectionSchedules));
            tasks.Add(context.AddRangeAsync(gpsDevices));
            tasks.Add(context.AddRangeAsync(bankAccounts));
            tasks.Add(context.AddRangeAsync(withdrawalRequests));
            tasks.Add(context.AddRangeAsync(bookings));
            tasks.Add(context.AddRangeAsync(transactions));
            tasks.Add(context.AddRangeAsync(reports));
            await Task.WhenAll(tasks);
            await context.SaveChangesAsync();
        }
        // Load init data to initial objects.
        UserRolesData userRolesData = app.ApplicationServices.GetRequiredService<UserRolesData>();
        userRolesData.Set(userRoles);
    }
}