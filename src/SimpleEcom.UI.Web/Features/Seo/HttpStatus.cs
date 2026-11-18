using Microsoft.AspNetCore.Diagnostics;

namespace SimpleEcom.UI.Web.Features.Seo;

/// <summary>
/// Códigos HTTP correctos para los buscadores en páginas renderizadas en servidor:
/// 404 para lo que no existe (evita "404 falsos" indexados) y 503 cuando la Api falla (el buscador reintenta).
/// Solo funciona en páginas sin streaming: con streaming las cabeceras ya se enviaron.
/// </summary>
/// <remarks>
/// La página ya muestra su propio mensaje ("Este producto no está disponible", "No podemos cargar…"), así que se
/// desactiva para esta petición UseStatusCodePagesWithReExecute: si no, sustituiría ese contenido por /not-found.
/// </remarks>
public static class HttpStatus
{
    public static void SetNotFound(HttpContext? httpContext) => Set(httpContext, StatusCodes.Status404NotFound);

    public static void SetUnavailable(HttpContext? httpContext) => Set(httpContext, StatusCodes.Status503ServiceUnavailable);

    private static void Set(HttpContext? httpContext, int statusCode)
    {
        if (httpContext is not { Response.HasStarted: false })
        {
            return;
        }

        httpContext.Response.StatusCode = statusCode;
        if (httpContext.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
        {
            statusCodePages.Enabled = false;
        }
    }
}
