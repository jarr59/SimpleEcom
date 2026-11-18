using SimpleEcom.Api.Domain.Branches;
using SimpleEcom.Shared.Contracts.Branches;

namespace SimpleEcom.Api.Features.Branches;

internal static class BranchMappings
{
    public static BranchResponse ToResponse(this Branch branch) => new(
        branch.Id.Value,
        branch.Code,
        branch.Name,
        branch.Address,
        branch.Phone,
        branch.AllowsPickup,
        branch.AllowsDelivery,
        branch.IsActive);
}
