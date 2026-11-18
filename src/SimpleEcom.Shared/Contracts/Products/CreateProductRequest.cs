namespace SimpleEcom.Shared.Contracts.Products;

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    Currency Currency,
    int Stock);
