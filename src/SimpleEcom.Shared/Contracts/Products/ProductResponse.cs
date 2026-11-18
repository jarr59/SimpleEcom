namespace SimpleEcom.Shared.Contracts.Products;

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    Currency Currency,
    int Stock,
    bool IsActive);
