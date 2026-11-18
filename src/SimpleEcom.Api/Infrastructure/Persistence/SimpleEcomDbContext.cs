using Microsoft.EntityFrameworkCore;
using SimpleEcom.Api.Domain.Products;

namespace SimpleEcom.Api.Infrastructure.Persistence;

public sealed class SimpleEcomDbContext(DbContextOptions<SimpleEcomDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimpleEcomDbContext).Assembly);
}
