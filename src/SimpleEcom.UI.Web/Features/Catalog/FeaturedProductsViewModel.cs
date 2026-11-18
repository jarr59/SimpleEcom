using SimpleEcom.Shared.Contracts.Products;
using SimpleEcom.Shared.Http;
using SimpleEcom.UI.Web.Common;

namespace SimpleEcom.UI.Web.Features.Catalog;

/// <summary>Selección de productos para la portada: activos y con stock.</summary>
public sealed class FeaturedProductsViewModel : ViewModelBase
{
    public const int MaxItems = 4;

    private readonly ProductsApiClient _api;

    public FeaturedProductsViewModel(ProductsApiClient api)
    {
        _api = api;
        Load = new AsyncOperation(Notify);
    }

    public AsyncOperation Load { get; }

    /// <summary>Null mientras no ha terminado la carga.</summary>
    public IReadOnlyList<ProductResponse>? Products { get; private set; }

    public string? ErrorMessage => Load.Error;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (Load.IsBusy)
        {
            return;
        }

        var result = await Load.RunAsync(() => _api.GetProductsAsync(isActive: true, cancellationToken: cancellationToken));
        Products = result.IsSuccess
            ? [.. result.Value.Where(p => p.IsActive && p.Stock > 0).Take(MaxItems)]
            : [];
        Notify();
    }
}
