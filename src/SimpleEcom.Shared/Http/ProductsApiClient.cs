using System.Net.Http.Json;
using SimpleEcom.Shared.Contracts.Products;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Shared.Http;

/// <summary>Cliente tipado de la Api, compartido por SimpleEcom.UI.Web y SimpleEcom.UI.Web.Admin.</summary>
public sealed class ProductsApiClient(HttpClient httpClient)
{
    private const string BasePath = "api/products";

    /// <param name="search">Texto a buscar en nombre o SKU.</param>
    /// <param name="isActive">Filtra por estado; null devuelve todos.</param>
    public async Task<Result<IReadOnlyList<ProductResponse>>> GetProductsAsync(
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
        var result = await ApiResponseReader.SendAsync<List<ProductResponse>>(ct => httpClient.GetAsync(url, ct), cancellationToken);

        return result.IsSuccess
            ? Result.Success<IReadOnlyList<ProductResponse>>(result.Value)
            : Result.Failure<IReadOnlyList<ProductResponse>>(result.Error);
    }

    public Task<Result<ProductResponse>> GetProductAsync(Guid id, CancellationToken cancellationToken = default) =>
        ApiResponseReader.SendAsync<ProductResponse>(
            ct => httpClient.GetAsync($"{BasePath}/{id}", ct), cancellationToken);

    public Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default) =>
        ApiResponseReader.SendAsync<ProductResponse>(
            ct => httpClient.PostAsJsonAsync(BasePath, request, ct), cancellationToken);

    public Task<Result<ProductResponse>> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default) =>
        ApiResponseReader.SendAsync<ProductResponse>(
            ct => httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct), cancellationToken);

    public Task<Result<ProductResponse>> SetProductActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default) =>
        ApiResponseReader.SendAsync<ProductResponse>(
            ct => httpClient.PostAsync($"{BasePath}/{id}/{(isActive ? "activate" : "deactivate")}", content: null, ct),
            cancellationToken);

    public Task<Result> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default) =>
        ApiResponseReader.SendAsync(
            ct => httpClient.DeleteAsync($"{BasePath}/{id}", ct), cancellationToken);
}
