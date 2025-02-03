using System.ComponentModel.DataAnnotations.Schema;
using Domain.Shared;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Domain.Entities;

public class Model : BaseEntity
{
    public required Guid ManufacturerId { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset ReleaseDate { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(ManufacturerId))]
    public Manufacturer Manufacturer { get; set; } = null!;
    public ICollection<Car> Cars { get; set; } = [];
}
