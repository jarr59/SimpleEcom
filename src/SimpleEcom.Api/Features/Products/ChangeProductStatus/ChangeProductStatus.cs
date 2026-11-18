using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Common.Http;
using SimpleEcom.Api.Domain.Products;
using SimpleEcom.Shared.Contracts.Products;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Features.Products.ChangeProductStatus;

/// <summary>Activa o desactiva un producto (un producto inactivo no se muestra en la tienda).</summary>
public sealed class ChangeProductStatusHandler(IProductRepository repository)
{
    public async Task<Result<ProductResponse>> HandleAsync(Guid id, bool activate, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(id);
        var product = await repository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound(productId);
        }

        if (activate)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

        await repository.UpdateAsync(product, cancellationToken);
        return product.ToResponse();
    }
}

public sealed class ChangeProductStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("products/{id:guid}/activate", (Guid id, ChangeProductStatusHandler handler, CancellationToken ct) =>
                Handle(handler, id, activate: true, ct))
            .WithName("ActivateProduct")
            .WithTags("Products")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPost("products/{id:guid}/deactivate", (Guid id, ChangeProductStatusHandler handler, CancellationToken ct) =>
                Handle(handler, id, activate: false, ct))
            .WithName("DeactivateProduct")
            .WithTags("Products")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(ChangeProductStatusHandler handler, Guid id, bool activate, CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, activate, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }
}
