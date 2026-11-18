using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Common.Http;
using SimpleEcom.Api.Domain.Branches;
using SimpleEcom.Shared.Contracts.Branches;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Features.Branches.GetBranchById;

public sealed class GetBranchByIdHandler(IBranchRepository repository)
{
    public async Task<Result<BranchResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var branchId = new BranchId(id);
        var branch = await repository.GetByIdAsync(branchId, cancellationToken);
        return branch is null ? BranchErrors.NotFound(branchId) : branch.ToResponse();
    }
}

public sealed class GetBranchByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("branches/{id:guid}", async (Guid id, GetBranchByIdHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .WithName("GetBranchById")
            .WithTags("Branches")
            .Produces<BranchResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
}
