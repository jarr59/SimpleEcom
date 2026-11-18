using SimpleEcom.Shared.Http;
using SimpleEcom.UI.Web.Admin.Common;

namespace SimpleEcom.UI.Web.Admin.Features.Products;

/// <summary>
/// Estado y lógica de alta y edición de un producto. La navegación tras guardar la decide la página,
/// así el ViewModel no depende de NavigationManager y se prueba sin Blazor.
/// </summary>
public sealed class ProductEditorViewModel : ViewModelBase
{
    private readonly ProductsApiClient _api;

    public ProductEditorViewModel(ProductsApiClient api)
    {
        _api = api;
        Load = new AsyncOperation(Notify);
        Save = new AsyncOperation(Notify);
    }

    public ProductFormModel Form { get; private set; } = new();

    /// <summary>Null en alta; Id del producto en edición.</summary>
    public Guid? ProductId { get; private set; }
    public bool IsEdit => ProductId is not null;

    /// <summary>Solo tiene sentido en edición: estado actual del producto cargado.</summary>
    public bool IsActive { get; private set; }

    /// <summary>En edición: el producto se cargó correctamente y el formulario está listo.</summary>
    public bool IsLoaded { get; private set; }

    public AsyncOperation Load { get; }
    public AsyncOperation Save { get; }

    public string SubmitText => IsEdit ? "Guardar cambios" : "Crear producto";

    public void StartNew()
    {
        ProductId = null;
        Form = new ProductFormModel();
        IsActive = false;
        IsLoaded = true;
        Notify();
    }

    /// <returns>true si el producto se cargó; false si la Api devolvió error (ver <see cref="Load"/>.Error).</returns>
    public async Task<bool> LoadAsync(Guid id)
    {
        ProductId = id;
        IsLoaded = false;

        var result = await Load.RunAsync(() => _api.GetProductAsync(id));
        if (result.IsFailure)
        {
            Notify();
            return false;
        }

        Form = ProductFormModel.FromResponse(result.Value);
        IsActive = result.Value.IsActive;
        IsLoaded = true;
        Notify();
        return true;
    }

    /// <returns>true si se guardó; false si hubo error (ver <see cref="Save"/>.Error).</returns>
    public async Task<bool> SaveAsync()
    {
        if (Save.IsBusy)
        {
            return false;
        }

        var result = await Save.RunAsync(() => ProductId is { } id
            ? _api.UpdateProductAsync(id, Form.ToUpdateRequest())
            : _api.CreateProductAsync(Form.ToCreateRequest()));

        return result.IsSuccess;
    }
}
