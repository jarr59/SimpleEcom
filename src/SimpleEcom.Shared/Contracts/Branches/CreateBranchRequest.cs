namespace SimpleEcom.Shared.Contracts.Branches;

public sealed record CreateBranchRequest(
    string Code,
    string Name,
    string Address,
    string? Phone,
    bool AllowsPickup,
    bool AllowsDelivery);
