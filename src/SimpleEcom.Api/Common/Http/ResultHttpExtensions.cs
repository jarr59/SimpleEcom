using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Common.Http;

/// <summary>Traduce errores de dominio a respuestas HTTP ProblemDetails.</summary>
public static class ResultHttpExtensions
{
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("No se puede convertir un resultado exitoso en un problema.");
        }

        var statusCode = result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            statusCode: statusCode,
            title: result.Error.Code,
            detail: result.Error.Description);
    }
}
