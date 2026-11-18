using SimpleEcom.Shared.Results;

namespace SimpleEcom.UI.Web.Common;

/// <summary>
/// Estado de una operación asíncrona contra la Api: ocupado + último error.
/// Centraliza el patrón "poner busy, llamar, capturar error, quitar busy" para no repetirlo en cada ViewModel.
/// </summary>
public sealed class AsyncOperation(Action? onChanged = null)
{
    public bool IsBusy { get; private set; }

    /// <summary>Descripción del último error, lista para mostrar al usuario. Null si la última ejecución fue bien.</summary>
    public string? Error { get; private set; }

    public void ClearError()
    {
        if (Error is null)
        {
            return;
        }

        Error = null;
        onChanged?.Invoke();
    }

    public async Task<Result<T>> RunAsync<T>(Func<Task<Result<T>>> action)
    {
        Begin();
        try
        {
            var result = await action();
            if (result.IsFailure)
            {
                Error = result.Error.Description;
            }

            return result;
        }
        finally
        {
            End();
        }
    }

    public async Task<Result> RunAsync(Func<Task<Result>> action)
    {
        Begin();
        try
        {
            var result = await action();
            if (result.IsFailure)
            {
                Error = result.Error.Description;
            }

            return result;
        }
        finally
        {
            End();
        }
    }

    private void Begin()
    {
        IsBusy = true;
        Error = null;
        onChanged?.Invoke();
    }

    private void End()
    {
        IsBusy = false;
        onChanged?.Invoke();
    }
}
