using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Shared.Http;

/// <summary>
/// Envía peticiones a la Api y convierte la respuesta (éxito o ProblemDetails) en <see cref="Result"/>.
/// Los fallos de red también se devuelven como <see cref="Error"/> para que la UI pueda mostrarlos.
/// </summary>
internal static class ApiResponseReader
{
    private static readonly Error Unavailable =
        new("Api.Unavailable", "No se ha podido conectar con la Api. Comprueba que está en ejecución.");

    private static readonly Error EmptyResponse =
        new("Api.EmptyResponse", "La Api devolvió una respuesta vacía.");

    public static async Task<Result<T>> SendAsync<T>(
        Func<CancellationToken, Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await send(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return await ReadErrorAsync(response, cancellationToken);
            }

            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return value is null ? Result.Failure<T>(EmptyResponse) : Result.Success(value);
        }
        catch (HttpRequestException)
        {
            return Unavailable;
        }
    }

    public static async Task<Result> SendAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await send(cancellationToken);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await ReadErrorAsync(response, cancellationToken));
        }
        catch (HttpRequestException)
        {
            return Result.Failure(Unavailable);
        }
    }

    private static async Task<Error> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ProblemPayload? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // La respuesta no es ProblemDetails: se usa un error genérico.
        }

        var type = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => ErrorType.Validation,
            HttpStatusCode.NotFound => ErrorType.NotFound,
            HttpStatusCode.Conflict => ErrorType.Conflict,
            _ => ErrorType.Failure
        };

        return new Error(
            problem?.Title ?? $"Http.{(int)response.StatusCode}",
            problem?.Detail ?? $"La Api respondió con el estado {(int)response.StatusCode} ({response.ReasonPhrase}).",
            type);
    }

    private sealed record ProblemPayload(string? Title, string? Detail);
}
