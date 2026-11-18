namespace SimpleEcom.Shared.Contracts.Branches;

public sealed record BranchResponse(
    Guid Id,
    string Code,
    string Name,
    string Address,
    string? Phone,
    bool AllowsPickup,
    bool AllowsDelivery,
    bool IsActive);
