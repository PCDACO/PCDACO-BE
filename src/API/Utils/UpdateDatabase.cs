using Domain.Data;
using Domain.Entities;
using Domain.Shared;

using Microsoft.EntityFrameworkCore;

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
        var aesEncryptionService = scope.ServiceProvider.GetRequiredService<IAesEncryptionService>();
        var keyManageService = scope.ServiceProvider.GetRequiredService<IKeyManagementService>();
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.Database.EnsureDeleted();
        context.Database.Migrate();
        // Seed data
        Amenity[] amenities = AmenityGenerator.Execute();
        BankInfo[] bankInfos = BankInfoGenerator.Execute();
        BookingStatus[] bookingStatuses = BookingStatusGenerator.Execute();
        CarStatus[] carStatuses = CarStatusGenerator.Execute();
        CompensationStatus[] compensationStatuses = CompensationStatusGenerator.Execute();
        FuelType[] fuelTypes = FuelTypeGenerator.Execute();
        ImageType[] imageTypes = ImageTypeGenerator.Execute();
        Manufacturer[] manufacturers = ManufacturerGenerator.Execute();
        TransactionStatus[] transactionStatuses = TransactionStatusGenerator.Execute();
        TransactionType[] transactionTypes = TransactionTypeGenerator.Execute();
        TransmissionType[] transmissionTypes = TransmissionTypeGenerator.Execute();
        ContractStatus[] contractStatuses = ContractStatusGenerator.Execute();
        UserRole[] userRoles = UserRoleGenerator.Execute();
        WithdrawalRequestStatus[] withdrawalRequestStatuses =
            WithdrawalRequestStatusGenerator.Execute();
        Model[] models = ModelGenerator.Execute(manufacturers);
        InspectionStatus[] inspectionStatuses = InspectionStatusGenerator.Execute();
        DeviceStatus[] deviceStatuses = DeviceStatusGenerator.Execute();
        GPSDevice[] gpsDevices = GPSDeviceGenerator.Execute(deviceStatuses);
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
            carStatuses,
            encryptionSettings,
            aesEncryptionService,
            keyManageService,
            tokenService
        );
        // Generate inspection schedules
        InspectionSchedule[] inspectionSchedules = InspectionScheduleGenerator.Execute(
            cars,
            inspectionStatuses,
            carStatuses
        );
        List<Task> tasks = [];
        tasks.Add(context.AddRangeAsync(withdrawalRequestStatuses));
        tasks.Add(context.AddRangeAsync(userRoles));
        tasks.Add(context.AddRangeAsync(contractStatuses));
        tasks.Add(context.AddRangeAsync(amenities));
        tasks.Add(context.AddRangeAsync(bankInfos));
        tasks.Add(context.AddRangeAsync(bookingStatuses));
        tasks.Add(context.AddRangeAsync(carStatuses));
        tasks.Add(context.AddRangeAsync(compensationStatuses));
        tasks.Add(context.AddRangeAsync(fuelTypes));
        tasks.Add(context.AddRangeAsync(imageTypes));
        tasks.Add(context.AddRangeAsync(transactionStatuses));
        tasks.Add(context.AddRangeAsync(transactionTypes));
        tasks.Add(context.AddRangeAsync(transmissionTypes));
        tasks.Add(context.AddRangeAsync(manufacturers));
        tasks.Add(context.AddRangeAsync(models));
        tasks.Add(context.AddRangeAsync(inspectionStatuses));
        tasks.Add(context.AddRangeAsync(deviceStatuses));
        tasks.Add(context.AddRangeAsync(users));
        tasks.Add(context.AddRangeAsync(cars));
        tasks.Add(context.AddRangeAsync(inspectionSchedules));
        tasks.Add(context.AddRangeAsync(gpsDevices));
        await Task.WhenAll(tasks);
        await context.SaveChangesAsync();
        // Load init data to initial objects.
        DeviceStatusesData gpsData = app.ApplicationServices.GetRequiredService<DeviceStatusesData>();
        gpsData.SetStatuses(deviceStatuses);
        TransactionStatusesData transactionStatusesData = app.ApplicationServices.GetRequiredService<TransactionStatusesData>();
        transactionStatusesData.Set(transactionStatuses);
        UserRolesData userRolesData = app.ApplicationServices.GetRequiredService<UserRolesData>();
        userRolesData.Set(userRoles);
        InspectionStatusesData inspectionStatusesData = app.ApplicationServices.GetRequiredService<InspectionStatusesData>();
        inspectionStatusesData.SetStatuses(inspectionStatuses);
    }
}
