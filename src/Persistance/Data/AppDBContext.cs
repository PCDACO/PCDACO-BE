using Microsoft.EntityFrameworkCore;

namespace Persistance.Data;

public class AppDBContext(DbContextOptions context) : DbContext(context)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}