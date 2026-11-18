using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Common.Http;
using SimpleEcom.Api.Domain.Products;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Features.Products.DeleteProduct;

public sealed class DeleteProductHandler(IProductRepository repository)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(id);
        var product = await repository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(productId));
        }

        await repository.RemoveAsync(product, cancellationToken);
        return Result.Success();
    }
}

public sealed class DeleteProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("products/{id:guid}", async (Guid id, DeleteProductHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .WithName("DeleteProduct")
            .WithTags("Products")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
