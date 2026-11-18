using Microsoft.AspNetCore.Mvc;
using SimpleEcom.Shared.Http;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.UI.Web.Features.Branches;

/// <summary>
/// Guarda la sucursal elegida en la cookie y vuelve a la página de origen.
/// Es un endpoint HTTP (no un componente) porque escribir cookies exige hacerlo antes de empezar a enviar
/// la respuesta, y las páginas de la tienda se envían en streaming. Funciona sin JavaScript: POST + redirección.
/// </summary>
public static class BranchSelectionEndpoint
{
    public const string Path = "branches/select";

    public static IEndpointRouteBuilder MapBranchSelection(this IEndpointRouteBuilder app)
    {
        // [FromForm] activa la validación antiforgery: el formulario incluye <AntiforgeryToken />.
        app.MapPost(Path, async (
                [FromForm] Guid branchId,
                [FromForm] string? returnUrl,
                BranchesApiClient api,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var branch = await api.GetBranchAsync(branchId, cancellationToken);

                if (branch.IsFailure)
                {
                    var status = branch.Error.Type == ErrorType.NotFound
                        ? BranchPickerViewModel.StatusUnavailable
                        : BranchPickerViewModel.StatusError;
                    return Results.LocalRedirect($"~/branches?status={status}");
                }

                if (!branch.Value.IsActive)
                {
                    return Results.LocalRedirect($"~/branches?status={BranchPickerViewModel.StatusUnavailable}");
                }

                BranchCookie.Write(httpContext.Response, branchId);
                return Results.LocalRedirect(SafeReturnUrl(returnUrl));
            })
            .ExcludeFromDescription();

        return app;
    }

    /// <summary>
    /// Solo se vuelve a rutas locales ("/products?q=x"). Cualquier otra cosa ("//evil.com", "https://…", "/\evil")
    /// redirige a la portada: evita usar la tienda como trampolín de redirecciones abiertas.
    /// </summary>
    public static string SafeReturnUrl(string? returnUrl)
    {
        const string Home = "~/";

        if (string.IsNullOrWhiteSpace(returnUrl) || returnUrl[0] != '/')
        {
            return Home;
        }

        if (returnUrl.Length > 1 && returnUrl[1] is '/' or '\\')
        {
            return Home;
        }

        return returnUrl.Any(char.IsControl) ? Home : returnUrl;
    }
}
