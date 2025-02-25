using Domain.Constants.EntityNames;
using Domain.Entities;

namespace Persistance.Bogus;

public class TransmissionTypeGenerator
{
    private static readonly string[] _transmissionTypes = [
        TransmissionTypeNames.Automatic,
        TransmissionTypeNames.Manual
    ];
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