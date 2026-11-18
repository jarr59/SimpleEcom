using SimpleEcom.Shared.Contracts.Products;
using SimpleEcom.Shared.Http;
using SimpleEcom.Shared.Results;
using SimpleEcom.UI.Web.Common;

namespace SimpleEcom.UI.Web.Features.Catalog;

/// <summary>Ficha de producto y sugerencias de otros productos.</summary>
public sealed class ProductDetailViewModel : ViewModelBase
{
    public const int MaxRelated = 4;

    private readonly ProductsApiClient _api;

    public ProductDetailViewModel(ProductsApiClient api)
    {
        _api = api;
        Load = new AsyncOperation(Notify);
        LoadRelated = new AsyncOperation(Notify);
    }

    public AsyncOperation Load { get; }

    /// <summary>Sugerencias: si fallan, la ficha se muestra igualmente sin ellas.</summary>
    public AsyncOperation LoadRelated { get; }

    public ProductResponse? Product { get; private set; }

    public IReadOnlyList<ProductResponse> Related { get; private set; } = [];

    /// <summary>El producto no existe o está desactivado: para el comprador es lo mismo.</summary>
    public bool IsNotFound { get; private set; }

    /// <summary>Error inesperado (p. ej. Api caída). Un "no encontrado" no es un error: se informa con <see cref="IsNotFound"/>.</summary>
    public string? ErrorMessage => IsNotFound ? null : Load.Error;

    public bool CanBuy => Product is { Stock: > 0 };

    public async Task LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (Load.IsBusy)
        {
            return;
        }

        Product = null;
        Related = [];
        IsNotFound = false;

        var result = await Load.RunAsync(() => _api.GetProductAsync(id, cancellationToken));
        if (result.IsSuccess && result.Value.IsActive)
        {
            Product = result.Value;
        }
        else
        {
            IsNotFound = result.IsSuccess || result.Error.Type == ErrorType.NotFound;
        }

        Notify();

        if (Product is not null)
        {
            await LoadRelatedAsync(Product.Id, cancellationToken);
        }
    }

    private async Task LoadRelatedAsync(Guid currentId, CancellationToken cancellationToken)
    {
        var result = await LoadRelated.RunAsync(() => _api.GetProductsAsync(isActive: true, cancellationToken: cancellationToken));
        if (result.IsSuccess)
        {
            Related = [.. result.Value
                .Where(p => p.IsActive && p.Id != currentId)
                .OrderBy(p => p.Stock <= 0)
                .Take(MaxRelated)];
        }

        Notify();
    }
}
