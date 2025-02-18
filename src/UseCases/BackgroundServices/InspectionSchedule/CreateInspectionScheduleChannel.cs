using MediatR;

using Microsoft.Extensions.Hosting;

namespace UseCases.BackgroundServices.InspectionSchedule;

public class CreateInspectionScheduleChannel(ISender sender) : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
            Console.WriteLine("Hello World");
        }
    }
}