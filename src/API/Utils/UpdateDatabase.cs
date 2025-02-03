using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistance.Bogus;
using Persistance.Data;

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
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.Database.EnsureDeleted();
        context.Database.Migrate();
        // Seed data
        Amenity[] amenities = AmenityGenerator.Execute();
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

        List<Task> tasks = [];

        tasks.Add(context.AddRangeAsync(withdrawalRequestStatuses));
        tasks.Add(context.AddRangeAsync(userRoles));
        tasks.Add(context.AddRangeAsync(contractStatuses));
        tasks.Add(context.AddRangeAsync(amenities));
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
        await Task.WhenAll(tasks);
        await context.SaveChangesAsync();
    }
}
