using Domain.Shared;

namespace Domain.Entities;

public class BankInfo : BaseEntity
{
    public required string BankLookUpId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public int Bin { get; set; }
    public required string ShortName { get; set; }
    public required string LogoUrl { get; set; }
    public required string IconUrl { get; set; }
    public required string SwiftCode { get; set; }
    public int LookupSupported { get; set; }
}
