namespace SimpleEcom.Shared.Contracts.Products;

/// <summary>Datos editables de un producto. El SKU no se puede modificar.</summary>
public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    Currency Currency,
    int Stock);
