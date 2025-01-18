using UUIDNext.Tools;

namespace UseCases.Utils;

public static class GetTimestampFromUuid
{
    public static DateTimeOffset Execute(Guid guid)
    {
        return UuidDecoder.TryDecodeTimestamp(guid, out DateTime timestamp) == false
            ? throw new ArgumentException("Invalid UUID")
            : (DateTimeOffset)timestamp;
    }
}