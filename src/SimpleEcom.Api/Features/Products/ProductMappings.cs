using SimpleEcom.Api.Domain.Products;
using SimpleEcom.Shared.Contracts.Products;

namespace SimpleEcom.Api.Features.Products;

internal static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product) => new(
        product.Id.Value,
        product.Sku,
        product.Name,
        product.Description,
        product.Price.Amount,
        product.Price.Currency,
        product.Stock,
        product.IsActive);
}
