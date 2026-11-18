using SimpleEcom.Shared.Contracts.Products;
using SimpleEcom.Shared.Http;
using SimpleEcom.UI.Web.Common;

namespace SimpleEcom.UI.Web.Features.Catalog;

/// <summary>Estado y lógica del catálogo: búsqueda, orden y filtro de disponibilidad. Solo muestra productos activos.</summary>
public sealed class CatalogViewModel : ViewModelBase
{
    private readonly ProductsApiClient _api;

    public CatalogViewModel(ProductsApiClient api)
    {
        _api = api;
        Load = new AsyncOperation(Notify);
    }

    public string? Search { get; set; }

    /// <summary>Uno de los valores de <see cref="CatalogSort"/>.</summary>
    public string Sort { get; set; } = CatalogSort.Default;

    /// <summary>Oculta los productos agotados.</summary>
    public bool OnlyAvailable { get; set; }

    public AsyncOperation Load { get; }

    /// <summary>Null mientras no ha terminado la primera carga.</summary>
    public IReadOnlyList<ProductResponse>? Products { get; private set; }

    public string? ErrorMessage => Load.Error;

    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || OnlyAvailable;

    public string Title => string.IsNullOrWhiteSpace(Search) ? "Catálogo" : $"Resultados para «{Search.Trim()}»";

    public string? Summary => Products?.Count switch
    {
        null or 0 => null,
        1 => "1 producto",
        var n => $"{n} productos"
    };

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (Load.IsBusy)
        {
            return;
        }

        Sort = CatalogSort.Normalize(Sort);
        var result = await Load.RunAsync(() => _api.GetProductsAsync(Search, isActive: true, cancellationToken));

        if (result.IsFailure)
        {
            Products = [];
        }
        else
        {
            // La Api ya filtra por activos; se repite por seguridad: un inactivo nunca debe verse en la tienda.
            var visible = result.Value.Where(p => p.IsActive && (!OnlyAvailable || p.Stock > 0));
            Products = [.. CatalogSort.Apply(visible, Sort)];
        }

        Notify();
    }
}
