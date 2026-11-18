namespace SimpleEcom.Shared.Contracts.Branches;

/// <summary>Datos editables de una sucursal. El código no se puede modificar.</summary>
public sealed record UpdateBranchRequest(
    string Name,
    string Address,
    string? Phone,
    bool AllowsPickup,
    bool AllowsDelivery);
