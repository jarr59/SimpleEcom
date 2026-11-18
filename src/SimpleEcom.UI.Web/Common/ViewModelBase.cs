namespace SimpleEcom.UI.Web.Common;

/// <summary>
/// Base de los ViewModels de la tienda. Contienen el estado y la lógica de una pantalla;
/// las páginas Razor solo renderizan y delegan. Se registran como Transient (uno por instancia de página).
/// </summary>
public abstract class ViewModelBase
{
    /// <summary>La página se suscribe para llamar a StateHasChanged cuando el estado cambia.</summary>
    public event Action? StateChanged;

    protected void Notify() => StateChanged?.Invoke();
}
