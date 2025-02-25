using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UseCases.Abstractions;
using UseCases.Services.EmailService;

namespace UseCases.BackgroundServices.Bookings;

public class ExpiredBookingNotificationService(IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IAppDBContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var expiredBookings = await context
                    .Bookings.Where(b => b.Status.Name == BookingStatusEnum.Expired.ToString())
                    .Include(b => b.User)
                    .Include(b => b.Car)
                    .ThenInclude(c => c.Model)
                    .ToListAsync(stoppingToken);

                foreach (var booking in expiredBookings)
                {
                    var driverEmailTemplate = DriverExpiredBookingTemplate.Template(
                        driverName: booking.User.Name,
                        carName: booking.Car.Model.Name,
                        startTime: booking.StartTime,
                        endTime: booking.EndTime,
                        totalAmount: booking.TotalAmount
                    );

                    var userEmail = booking.User.Email;

                    await emailService.SendEmailAsync(
                        userEmail,
                        "Đặt Xe Hết Hạn",
                        driverEmailTemplate
                    );
                }
            }

            await Task.Delay(_interval, stoppingToken); // Check every hour
        }
    }
}
