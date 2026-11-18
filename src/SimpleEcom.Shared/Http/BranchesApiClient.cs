using System.Net.Http.Json;
using SimpleEcom.Shared.Contracts.Branches;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Shared.Http;

/// <summary>Cliente tipado de las sucursales en la Api, compartido por la tienda y el panel de administración.</summary>
public sealed class BranchesApiClient(HttpClient httpClient)
{
    private const string BasePath = "api/branches";

    /// <param name="search">Texto a buscar en nombre o código.</param>
    /// <param name="isActive">Filtra por estado; null devuelve todas.</param>
    public async Task<Result<IReadOnlyList<BranchResponse>>> GetBranchesAsync(
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        if (isActive is not null)
        {
            query.Add($"isActive={(isActive.Value ? "true" : "false")}");
        }

        var url = query.Count == 0 ? BasePath : $"{BasePath}?{string.Join('&', query)}";
        var result = await ApiResponseReader.SendAsync<List<BranchResponse>>(ct => httpClient.GetAsync(url, ct), cancellationToken);

        return result.IsSuccess
            ? Result.Success<IReadOnlyList<BranchResponse>>(result.Value)
            : Result.Failure<IReadOnlyList<BranchResponse>>(result.Error);
    }

    public Task<Result<BranchResponse>> GetBranchAsync(Guid id, CancellationToken cancellationToken = default) =>
        ApiResponseReader.SendAsync<BranchResponse>(
            ct => httpClient.GetAsync($"{BasePath}/{id}", ct), cancellationToken);

    public Task<Result<BranchResponse>> CreateBranchAsync(CreateBranchRequest request, CancellationToken cancellationToken = default) =>
        ApiResponseReader.SendAsync<BranchResponse>(
            ct => httpClient.PostAsJsonAsync(BasePath, request, ct), cancellationToken);

    public Task<Result<BranchResponse>> UpdateBranchAsync(Guid id, UpdateBranchRequest request, CancellationToken cancellationToken = default) =>
        ApiResponseReader.SendAsync<BranchResponse>(
            ct => httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct), cancellationToken);

    public Task<Result<BranchResponse>> SetBranchActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default) =>
        ApiResponseReader.SendAsync<BranchResponse>(
            ct => httpClient.PostAsync($"{BasePath}/{id}/{(isActive ? "activate" : "deactivate")}", content: null, ct),
            cancellationToken);

    public Task<Result> DeleteBranchAsync(Guid id, CancellationToken cancellationToken = default) =>
        ApiResponseReader.SendAsync(
            ct => httpClient.DeleteAsync($"{BasePath}/{id}", ct), cancellationToken);
}
