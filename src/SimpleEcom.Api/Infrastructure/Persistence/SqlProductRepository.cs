using Microsoft.EntityFrameworkCore;
using SimpleEcom.Api.Domain.Products;

namespace SimpleEcom.Api.Infrastructure.Persistence;

/// <summary>Repositorio de productos sobre SQL Server (EF Core). Cada operación de escritura confirma sus cambios.</summary>
public sealed class SqlProductRepository(SimpleEcomDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default) =>
        dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Products.OrderBy(p => p.Name).ToListAsync(cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default)
    {
        var normalized = Product.NormalizeSku(sku);
        return dbContext.Products.AnyAsync(p => p.Sku == normalized, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        dbContext.Products.Update(product);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Product product, CancellationToken cancellationToken = default)
    {
        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
