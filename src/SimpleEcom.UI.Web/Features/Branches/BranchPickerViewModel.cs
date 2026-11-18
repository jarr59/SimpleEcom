using SimpleEcom.Shared.Contracts.Branches;
using SimpleEcom.Shared.Http;
using SimpleEcom.UI.Web.Common;

namespace SimpleEcom.UI.Web.Features.Branches;

/// <summary>Página de sucursales: el cliente ve dónde puede comprar y elige la suya.</summary>
public sealed class BranchPickerViewModel : ViewModelBase
{
    /// <summary>Valores de ?status= con los que vuelve el endpoint de selección cuando no pudo guardar.</summary>
    public const string StatusUnavailable = "unavailable";
    public const string StatusError = "error";

    private readonly BranchesApiClient _api;

    public BranchPickerViewModel(BranchesApiClient api)
    {
        _api = api;
        Load = new AsyncOperation(Notify);
    }

    public AsyncOperation Load { get; }

    /// <summary>Null mientras no ha terminado la carga.</summary>
    public IReadOnlyList<BranchResponse>? Branches { get; private set; }

    public Guid? CurrentBranchId { get; private set; }

    /// <summary>Ruta local a la que volver tras elegir (se valida de nuevo en el servidor).</summary>
    public string? ReturnUrl { get; private set; }

    /// <summary>Aviso cuando la elección anterior no se pudo guardar.</summary>
    public string? StatusMessage { get; private set; }

    public string? ErrorMessage => Load.Error;

    public bool IsCurrent(Guid branchId) => CurrentBranchId == branchId;

    public void Initialize(Guid? currentBranchId, string? returnUrl, string? status)
    {
        CurrentBranchId = currentBranchId;
        ReturnUrl = returnUrl;
        StatusMessage = status switch
        {
            StatusUnavailable => "Esa sucursal ya no está disponible. Elige otra, por favor.",
            StatusError => "No pudimos guardar tu sucursal. Inténtalo de nuevo en unos minutos.",
            _ => null
        };
        Notify();
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (Load.IsBusy)
        {
            return;
        }

        var result = await Load.RunAsync(() => _api.GetBranchesAsync(isActive: true, cancellationToken: cancellationToken));

        // La sucursal actual primero; el resto, en el orden de la Api (por nombre).
        Branches = result.IsSuccess
            ? [.. result.Value.Where(b => b.IsActive).OrderBy(b => !IsCurrent(b.Id))]
            : [];
        Notify();
    }

    /// <summary>Texto de los modos de entrega para lectores de pantalla y tarjetas.</summary>
    public static IReadOnlyList<string> FulfillmentLabels(BranchResponse branch)
    {
        var labels = new List<string>(2);
        if (branch.AllowsPickup)
        {
            labels.Add("Retiro en tienda");
        }

        if (branch.AllowsDelivery)
        {
            labels.Add("Envío a domicilio");
        }

        return labels;
    }

    /// <summary>Enlace tel: con solo dígitos y '+', para que funcione en móviles.</summary>
    public static string? PhoneHref(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = string.Concat(phone.Where(c => char.IsDigit(c) || c == '+'));
        return digits.Length == 0 ? null : $"tel:{digits}";
    }
}
