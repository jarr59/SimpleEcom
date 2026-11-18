using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Common.Http;
using SimpleEcom.Api.Domain.Products;
using SimpleEcom.Shared.Contracts.Products;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Features.Products.CreateProduct;

public sealed class CreateProductHandler(IProductRepository repository, TimeProvider timeProvider)
{
    public async Task<Result<ProductResponse>> HandleAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var price = Money.Create(request.Price, request.Currency);
        if (price.IsFailure)
        {
            return price.Error;
        }

        var product = Product.Create(request.Sku, request.Name, request.Description, price.Value, request.Stock, timeProvider);
        if (product.IsFailure)
        {
            return product.Error;
        }

        if (await repository.SkuExistsAsync(product.Value.Sku, cancellationToken))
        {
            return ProductErrors.SkuAlreadyExists(product.Value.Sku);
        }

        await repository.AddAsync(product.Value, cancellationToken);
        return product.Value.ToResponse();
    }
}

public sealed class CreateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("products", async (CreateProductRequest request, CreateProductHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/products/{result.Value.Id}", result.Value)
                    : result.ToProblem();
            })
            .WithName("CreateProduct")
            .WithTags("Products")
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
}
