using Microsoft.EntityFrameworkCore;
using SimpleEcom.Api.Domain.Branches;
using SimpleEcom.Api.Domain.Products;

namespace SimpleEcom.Api.Infrastructure.Persistence;

public sealed class SimpleEcomDbContext(DbContextOptions<SimpleEcomDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Branch> Branches => Set<Branch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimpleEcomDbContext).Assembly);
}
