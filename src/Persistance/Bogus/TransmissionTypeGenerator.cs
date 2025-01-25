using Domain.Entities;

namespace Persistance.Bogus;

public class TransmissionTypeGenerator
{
    private static readonly string[] _transmissionTypes = ["Automatic", "Manual"];
    public static TransmissionType[] Execute()
    {
        return [.. _transmissionTypes.Select(status => {
            return new TransmissionType()
            {
                Name = status,
            };
        })];
    }
}