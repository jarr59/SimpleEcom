using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Common.Http;
using SimpleEcom.Api.Domain.Branches;
using SimpleEcom.Shared.Contracts.Branches;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Features.Branches.UpdateBranch;

public sealed class UpdateBranchHandler(IBranchRepository repository)
{
    public async Task<Result<BranchResponse>> HandleAsync(Guid id, UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var branchId = new BranchId(id);
        var branch = await repository.GetByIdAsync(branchId, cancellationToken);
        if (branch is null)
        {
            return BranchErrors.NotFound(branchId);
        }

        var update = branch.Update(request.Name, request.Address, request.Phone, request.AllowsPickup, request.AllowsDelivery);
        if (update.IsFailure)
        {
            return update.Error;
        }

        await repository.UpdateAsync(branch, cancellationToken);
        return branch.ToResponse();
    }
}

public sealed class UpdateBranchEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("branches/{id:guid}", async (Guid id, UpdateBranchRequest request, UpdateBranchHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .WithName("UpdateBranch")
            .WithTags("Branches")
            .Produces<BranchResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
