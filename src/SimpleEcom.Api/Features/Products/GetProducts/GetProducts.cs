using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Domain.Products;
using SimpleEcom.Shared.Contracts.Products;

namespace SimpleEcom.Api.Features.Products.GetProducts;

public sealed class GetProductsHandler(IProductRepository repository)
{
    /// <param name="search">Texto a buscar en nombre o SKU (opcional).</param>
    /// <param name="isActive">Filtra por estado; null devuelve todos.</param>
    public async Task<IReadOnlyList<ProductResponse>> HandleAsync(string? search = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var products = await repository.GetAllAsync(cancellationToken);

        var query = products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive is not null)
        {
            query = query.Where(p => p.IsActive == isActive);
        }

        return query.Select(p => p.ToResponse()).ToList();
    }
}

public sealed class GetProductsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("products", async (string? search, bool? isActive, GetProductsHandler handler, CancellationToken ct) =>
                Results.Ok(await handler.HandleAsync(search, isActive, ct)))
            .WithName("GetProducts")
            .WithTags("Products")
            .Produces<IReadOnlyList<ProductResponse>>();
}
