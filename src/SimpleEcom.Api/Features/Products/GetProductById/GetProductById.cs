using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Common.Http;
using SimpleEcom.Api.Domain.Products;
using SimpleEcom.Shared.Contracts.Products;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Features.Products.GetProductById;

public sealed class GetProductByIdHandler(IProductRepository repository)
{
    public async Task<Result<ProductResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(id);
        var product = await repository.GetByIdAsync(productId, cancellationToken);
        return product is null ? ProductErrors.NotFound(productId) : product.ToResponse();
    }
}

public sealed class GetProductByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("products/{id:guid}", async (Guid id, GetProductByIdHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .WithName("GetProductById")
            .WithTags("Products")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
}
