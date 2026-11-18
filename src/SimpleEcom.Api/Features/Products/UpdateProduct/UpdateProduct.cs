using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Common.Http;
using SimpleEcom.Api.Domain.Products;
using SimpleEcom.Shared.Contracts.Products;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Features.Products.UpdateProduct;

public sealed class UpdateProductHandler(IProductRepository repository)
{
    public async Task<Result<ProductResponse>> HandleAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(id);
        var product = await repository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(productId);
        }

        var price = Money.Create(request.Price, request.Currency);
        if (price.IsFailure)
        {
            return price.Error;
        }

        var update = product.Update(request.Name, request.Description, price.Value, request.Stock);
        if (update.IsFailure)
        {
            return update.Error;
        }

        await repository.UpdateAsync(product, cancellationToken);
        return product.ToResponse();
    }
}

public sealed class UpdateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("products/{id:guid}", async (Guid id, UpdateProductRequest request, UpdateProductHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .WithName("UpdateProduct")
            .WithTags("Products")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
