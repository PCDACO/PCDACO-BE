using Domain.Entities;

namespace Persistance.Bogus;

public class CompensationStatusGenerator
{
    private static readonly string[] _compensationStatus = ["Pending", "Resolved", "Rejected"];
    public static CompensationStatus[] Execute()
    {
        return [.. _compensationStatus.Select(status => {
            return new CompensationStatus()
            {
                Name = status,
            };
        })];
    }    
}