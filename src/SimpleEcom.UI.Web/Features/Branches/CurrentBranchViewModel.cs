using SimpleEcom.Shared.Contracts.Branches;
using SimpleEcom.Shared.Http;
using SimpleEcom.UI.Web.Common;

namespace SimpleEcom.UI.Web.Features.Branches;

/// <summary>
/// Sucursal en la que compra el cliente, para la barra bajo la cabecera.
/// Si la sucursal guardada ya no existe o está inactiva se trata como "sin elegir";
/// si solo hay una sucursal activa se asume esa sin preguntar.
/// </summary>
public sealed class CurrentBranchViewModel : ViewModelBase
{
    private readonly BranchesApiClient _api;

    public CurrentBranchViewModel(BranchesApiClient api)
    {
        _api = api;
        Load = new AsyncOperation(Notify);
    }

    public AsyncOperation Load { get; }

    /// <summary>Sucursales activas que puede elegir el cliente.</summary>
    public IReadOnlyList<BranchResponse> Branches { get; private set; } = [];

    public BranchResponse? Current { get; private set; }

    public bool IsLoaded { get; private set; }

    /// <summary>Hay varias sucursales y el cliente aún no ha elegido: se le invita a hacerlo.</summary>
    public bool NeedsSelection => IsLoaded && Current is null && Branches.Count > 0;

    /// <summary>Solo tiene sentido ofrecer "Cambiar" si hay más de una sucursal.</summary>
    public bool CanChange => Branches.Count > 1;

    /// <summary>Si la Api falla la barra no se muestra: la página ya informa del problema por su cuenta.</summary>
    public bool IsVisible => IsLoaded && Load.Error is null && Branches.Count > 0;

    public async Task LoadAsync(Guid? selectedBranchId, CancellationToken cancellationToken = default)
    {
        if (Load.IsBusy)
        {
            return;
        }

        var result = await Load.RunAsync(() => _api.GetBranchesAsync(isActive: true, cancellationToken: cancellationToken));

        Branches = result.IsSuccess ? [.. result.Value.Where(b => b.IsActive)] : [];
        Current = Branches.FirstOrDefault(b => b.Id == selectedBranchId)
            ?? (Branches.Count == 1 ? Branches[0] : null);

        IsLoaded = true;
        Notify();
    }
}
