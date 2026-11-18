using SimpleEcom.Shared.Contracts.Branches;
using SimpleEcom.Shared.Http;
using SimpleEcom.Shared.Results;
using SimpleEcom.UI.Web.Admin.Common;

namespace SimpleEcom.UI.Web.Admin.Features.Branches;

public enum BranchRowAction
{
    None,
    Toggle,
    Delete
}

/// <summary>Estado y lógica del listado de sucursales: filtros, carga, activar/desactivar y eliminar.</summary>
public sealed class BranchListViewModel : ViewModelBase
{
    private readonly BranchesApiClient _api;

    public BranchListViewModel(BranchesApiClient api)
    {
        _api = api;
        Load = new AsyncOperation(Notify);
        RowAction = new AsyncOperation(Notify);
    }

    public List<BranchResponse>? Branches { get; private set; }

    public string? Search { get; set; }

    /// <summary>"" = todas, "true" = activas, "false" = inactivas (valores del select).</summary>
    public string StatusFilter { get; set; } = string.Empty;

    public AsyncOperation Load { get; }
    public AsyncOperation RowAction { get; }

    public Guid? BusyBranchId { get; private set; }
    public BranchRowAction BusyAction { get; private set; } = BranchRowAction.None;
    public BranchResponse? BranchToDelete { get; private set; }

    public string? SuccessMessage { get; private set; }
    public string? ErrorMessage => Load.Error ?? RowAction.Error;

    /// <summary>Hay una carga o una acción de fila en curso: la página bloquea el resto de controles.</summary>
    public bool IsAnyBusy => Load.IsBusy || RowAction.IsBusy;

    public bool IsRowBusy(Guid branchId, BranchRowAction action) =>
        RowAction.IsBusy && BusyBranchId == branchId && BusyAction == action;

    /// <summary>Texto de los modos de entrega que ofrece la sucursal, para la tabla.</summary>
    public static string FulfillmentText(BranchResponse branch) => (branch.AllowsPickup, branch.AllowsDelivery) switch
    {
        (true, true) => "Retiro y envío",
        (true, false) => "Solo retiro",
        (false, true) => "Solo envío",
        _ => "Sin modos de entrega"
    };

    /// <summary>Traduce el parámetro ?status= de la URL (tras crear/editar) a un mensaje de éxito.</summary>
    public void ShowStatus(string? status)
    {
        SuccessMessage = status switch
        {
            "created" => "Sucursal creada correctamente.",
            "updated" => "Sucursal actualizada correctamente.",
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
        var result = await Load.RunAsync(() => _api.GetBranchesAsync(Search, isActive));

        Branches = result.IsSuccess ? [.. result.Value] : [];
        Notify();
    }

    public async Task ToggleActiveAsync(BranchResponse branch)
    {
        if (IsAnyBusy)
        {
            return;
        }

        SuccessMessage = null;
        var result = await RunRowActionAsync(branch.Id, BranchRowAction.Toggle,
            () => _api.SetBranchActiveAsync(branch.Id, !branch.IsActive));

        if (result.IsSuccess)
        {
            SuccessMessage = result.Value.IsActive
                ? $"«{result.Value.Name}» activada."
                : $"«{result.Value.Name}» desactivada.";
            await LoadAsync();
        }
    }

    public void RequestDelete(BranchResponse branch)
    {
        if (IsAnyBusy)
        {
            return;
        }

        BranchToDelete = branch;
        Notify();
    }

    public void CancelDelete()
    {
        if (RowAction.IsBusy)
        {
            return;
        }

        BranchToDelete = null;
        Notify();
    }

    public async Task ConfirmDeleteAsync()
    {
        if (BranchToDelete is null || IsAnyBusy)
        {
            return;
        }

        var branch = BranchToDelete;
        SuccessMessage = null;
        var result = await RunRowActionAsync(branch.Id, BranchRowAction.Delete,
            () => _api.DeleteBranchAsync(branch.Id));

        BranchToDelete = null;
        if (result.IsSuccess)
        {
            SuccessMessage = $"«{branch.Name}» eliminada.";
            await LoadAsync();
        }
        else
        {
            Notify();
        }
    }

    private async Task<Result<T>> RunRowActionAsync<T>(Guid branchId, BranchRowAction action, Func<Task<Result<T>>> call)
    {
        BusyBranchId = branchId;
        BusyAction = action;
        try
        {
            return await RowAction.RunAsync(call);
        }
        finally
        {
            BusyBranchId = null;
            BusyAction = BranchRowAction.None;
        }
    }

    private async Task<Result> RunRowActionAsync(Guid branchId, BranchRowAction action, Func<Task<Result>> call)
    {
        BusyBranchId = branchId;
        BusyAction = action;
        try
        {
            return await RowAction.RunAsync(call);
        }
        finally
        {
            BusyBranchId = null;
            BusyAction = BranchRowAction.None;
        }
    }
}
