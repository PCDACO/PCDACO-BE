namespace Domain.Shared;

public class SmsSettings
{
    public required string AccountSid { get; set; }
    public required string AuthToken { get; set; }
    public required string PhoneNumber { get; set; }
}