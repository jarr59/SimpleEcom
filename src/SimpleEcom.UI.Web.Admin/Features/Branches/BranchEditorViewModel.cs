using SimpleEcom.Shared.Http;
using SimpleEcom.UI.Web.Admin.Common;

namespace SimpleEcom.UI.Web.Admin.Features.Branches;

/// <summary>
/// Estado y lógica de alta y edición de una sucursal. La navegación tras guardar la decide la página,
/// así el ViewModel no depende de NavigationManager y se prueba sin Blazor.
/// </summary>
public sealed class BranchEditorViewModel : ViewModelBase
{
    private readonly BranchesApiClient _api;

    public BranchEditorViewModel(BranchesApiClient api)
    {
        _api = api;
        Load = new AsyncOperation(Notify);
        Save = new AsyncOperation(Notify);
    }

    public BranchFormModel Form { get; private set; } = new();

    /// <summary>Null en alta; Id de la sucursal en edición.</summary>
    public Guid? BranchId { get; private set; }
    public bool IsEdit => BranchId is not null;

    /// <summary>Solo tiene sentido en edición: estado actual de la sucursal cargada.</summary>
    public bool IsActive { get; private set; }

    /// <summary>En edición: la sucursal se cargó correctamente y el formulario está listo.</summary>
    public bool IsLoaded { get; private set; }

    public AsyncOperation Load { get; }
    public AsyncOperation Save { get; }

    public string SubmitText => IsEdit ? "Guardar cambios" : "Crear sucursal";

    public void StartNew()
    {
        BranchId = null;
        Form = new BranchFormModel();
        IsActive = false;
        IsLoaded = true;
        Notify();
    }

    /// <returns>true si la sucursal se cargó; false si la Api devolvió error (ver <see cref="Load"/>.Error).</returns>
    public async Task<bool> LoadAsync(Guid id)
    {
        BranchId = id;
        IsLoaded = false;

        var result = await Load.RunAsync(() => _api.GetBranchAsync(id));
        if (result.IsFailure)
        {
            Notify();
            return false;
        }

        Form = BranchFormModel.FromResponse(result.Value);
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

        var result = await Save.RunAsync(() => BranchId is { } id
            ? _api.UpdateBranchAsync(id, Form.ToUpdateRequest())
            : _api.CreateBranchAsync(Form.ToCreateRequest()));

        return result.IsSuccess;
    }
}
