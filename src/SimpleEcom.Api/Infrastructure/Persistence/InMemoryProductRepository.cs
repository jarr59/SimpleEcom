using System.Collections.Concurrent;
using SimpleEcom.Api.Domain.Products;

namespace SimpleEcom.Api.Infrastructure.Persistence;

/// <summary>Repositorio en memoria para arrancar. Sustituir por EF Core cuando se defina la base de datos.</summary>
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<ProductId, Product> _products = new();

    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.GetValueOrDefault(id));

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>(_products.Values.OrderBy(p => p.Name).ToList());

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.Values.Any(p => string.Equals(p.Sku, sku.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products.TryRemove(product.Id, out _);
        return Task.CompletedTask;
    }
}
