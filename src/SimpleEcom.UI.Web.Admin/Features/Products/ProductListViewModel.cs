using SimpleEcom.Shared.Contracts.Products;
using SimpleEcom.Shared.Http;
using SimpleEcom.UI.Web.Admin.Common;

namespace SimpleEcom.UI.Web.Admin.Features.Products;

public enum ProductRowAction
{
    None,
    Toggle,
    Delete
}

/// <summary>Estado y lógica del listado de productos: filtros, carga, activar/desactivar y eliminar.</summary>
public sealed class ProductListViewModel : ViewModelBase
{
    private readonly ProductsApiClient _api;

    public ProductListViewModel(ProductsApiClient api)
    {
        _api = api;
        Load = new AsyncOperation(Notify);
        RowAction = new AsyncOperation(Notify);
    }

    public List<ProductResponse>? Products { get; private set; }

    public string? Search { get; set; }

    /// <summary>"" = todos, "true" = activos, "false" = inactivos (valores del select).</summary>
    public string StatusFilter { get; set; } = string.Empty;

    public AsyncOperation Load { get; }
    public AsyncOperation RowAction { get; }

    public Guid? BusyProductId { get; private set; }
    public ProductRowAction BusyAction { get; private set; } = ProductRowAction.None;
    public ProductResponse? ProductToDelete { get; private set; }

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage => Load.Error ?? RowAction.Error;

    /// <summary>Hay una carga o una acción de fila en curso: la página bloquea el resto de controles.</summary>
    public bool IsAnyBusy => Load.IsBusy || RowAction.IsBusy;

    public bool IsRowBusy(Guid productId, ProductRowAction action) =>
        RowAction.IsBusy && BusyProductId == productId && BusyAction == action;

    /// <summary>Traduce el parámetro ?status= de la URL (tras crear/editar) a un mensaje de éxito.</summary>
    public void ShowStatus(string? status)
    {
        SuccessMessage = status switch
        {
            "created" => "Producto creado correctamente.",
            "updated" => "Producto actualizado correctamente.",
            _ => null
        };
        Notify();
    }

    public void DismissSuccess()
    {
        SuccessMessage = null;
        Notify();
    }

    public void DismissError()
    {
        Load.ClearError();
        RowAction.ClearError();
    }

    public async Task LoadAsync()
    {
        if (Load.IsBusy)
        {
            return;
        }

        bool? isActive = bool.TryParse(StatusFilter, out var value) ? value : null;
        var result = await Load.RunAsync(() => _api.GetProductsAsync(Search, isActive));

        Products = result.IsSuccess ? [.. result.Value] : [];
        Notify();
    }

    public async Task ToggleActiveAsync(ProductResponse product)
    {
        if (IsAnyBusy)
        {
            return;
        }

        SuccessMessage = null;
        var result = await RunRowActionAsync(product.Id, ProductRowAction.Toggle,
            () => _api.SetProductActiveAsync(product.Id, !product.IsActive));

        if (result.IsSuccess)
        {
            SuccessMessage = result.Value.IsActive
                ? $"«{result.Value.Name}» activado."
                : $"«{result.Value.Name}» desactivado.";
            await LoadAsync();
        }
    }

    public void RequestDelete(ProductResponse product)
    {
        if (IsAnyBusy)
        {
            return;
        }

        ProductToDelete = product;
        Notify();
    }

    public void CancelDelete()
    {
        if (RowAction.IsBusy)
        {
            return;
        }

        ProductToDelete = null;
        Notify();
    }

    public async Task ConfirmDeleteAsync()
    {
        if (ProductToDelete is null || IsAnyBusy)
        {
            return;
        }

        var product = ProductToDelete;
        SuccessMessage = null;
        var result = await RunRowActionAsync(product.Id, ProductRowAction.Delete,
            () => _api.DeleteProductAsync(product.Id));

        ProductToDelete = null;
        if (result.IsSuccess)
        {
            SuccessMessage = $"«{product.Name}» eliminado.";
            await LoadAsync();
        }
        else
        {
            Notify();
        }
    }

    private async Task<Shared.Results.Result<T>> RunRowActionAsync<T>(Guid productId, ProductRowAction action, Func<Task<Shared.Results.Result<T>>> call)
    {
        BusyProductId = productId;
        BusyAction = action;
        try
        {
            return await RowAction.RunAsync(call);
        }
        finally
        {
            BusyProductId = null;
            BusyAction = ProductRowAction.None;
        }
    }

    private async Task<Shared.Results.Result> RunRowActionAsync(Guid productId, ProductRowAction action, Func<Task<Shared.Results.Result>> call)
    {
        BusyProductId = productId;
        BusyAction = action;
        try
        {
            return await RowAction.RunAsync(call);
        }
        finally
        {
            BusyProductId = null;
            BusyAction = ProductRowAction.None;
        }
    }
}
