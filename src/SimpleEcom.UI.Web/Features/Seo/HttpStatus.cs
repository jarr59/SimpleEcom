using Microsoft.AspNetCore.Diagnostics;

namespace SimpleEcom.UI.Web.Features.Seo;

/// <summary>
/// 503 para los buscadores cuando la Api falla en una página renderizada en servidor (el buscador reintenta más tarde
/// en lugar de indexar el mensaje de error). Solo funciona en páginas sin streaming: con streaming las cabeceras ya se enviaron.
/// </summary>
/// <remarks>
/// Para el 404 NO se usa un código manual: Blazor descarta el HTML de una página que termina con 404 sin pasar por
/// <c>NavigationManager.NotFound()</c>, que es lo que hay que llamar (pinta <c>Pages/NotFound</c>).
/// Aquí se desactiva <c>UseStatusCodePagesWithReExecute</c> para esta petición: la página ya muestra su propio mensaje.
/// </remarks>
public static class HttpStatus
{
    public static void SetUnavailable(HttpContext? httpContext)
    {
        if (httpContext is not { Response.HasStarted: false })
        {
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        if (httpContext.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
        {
            statusCodePages.Enabled = false;
        }
    }
}
