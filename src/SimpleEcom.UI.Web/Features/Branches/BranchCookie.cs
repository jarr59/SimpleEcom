namespace SimpleEcom.UI.Web.Features.Branches;

/// <summary>
/// Cookie con la sucursal elegida por el comprador. Es una preferencia funcional (no rastrea al usuario):
/// solo guarda el Id de la sucursal para mostrar sus precios y modos de entrega en las siguientes visitas.
/// </summary>
/// <remarks>
/// Con HTTPS se usa el prefijo <c>__Host-</c>: el navegador solo la acepta si es Secure, con Path=/ y sin Domain,
/// así un subdominio o un atacante en la red no puede fijarla. Por HTTP (perfil "http" de desarrollo, sin redirección)
/// ese prefijo haría que el navegador la rechazara, por eso se usa un nombre sin prefijo.
/// </remarks>
public static class BranchCookie
{
    public const string SecureName = "__Host-simpleecom-branch";
    public const string InsecureName = "simpleecom-branch";

    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(365);

    public static string NameFor(HttpRequest request) => request.IsHttps ? SecureName : InsecureName;

    /// <returns>El Id guardado, o null si no hay cookie o su valor no es válido.</returns>
    public static Guid? Read(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return null;
        }

        var request = httpContext.Request;
        return request.Cookies.TryGetValue(NameFor(request), out var value) && Guid.TryParse(value, out var id)
            ? id
            : null;
    }

    public static void Write(HttpResponse response, Guid branchId)
    {
        var request = response.HttpContext.Request;
        response.Cookies.Append(NameFor(request), branchId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = Lifetime,
            Path = "/"
        });
    }
}
