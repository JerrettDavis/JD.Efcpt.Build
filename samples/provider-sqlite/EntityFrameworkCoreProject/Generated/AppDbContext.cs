// Committed reference output; configure the runtime connection in the host application.
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCoreProject.Generated;

public partial class Customer
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
}

public partial class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public virtual DbSet<Customer> Customers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
        });
    }
}
