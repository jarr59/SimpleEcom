using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Common.Http;
using SimpleEcom.Api.Domain.Branches;
using SimpleEcom.Shared.Contracts.Branches;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Features.Branches.ChangeBranchStatus;

/// <summary>Activa o desactiva una sucursal (una sucursal inactiva no se ofrece en la tienda).</summary>
public sealed class ChangeBranchStatusHandler(IBranchRepository repository)
{
    public async Task<Result<BranchResponse>> HandleAsync(Guid id, bool activate, CancellationToken cancellationToken = default)
    {
        var branchId = new BranchId(id);
        var branch = await repository.GetByIdAsync(branchId, cancellationToken);
        if (branch is null)
        {
            return BranchErrors.NotFound(branchId);
        }

        if (activate)
        {
            branch.Activate();
        }
        else
        {
            branch.Deactivate();
        }

        await repository.UpdateAsync(branch, cancellationToken);
        return branch.ToResponse();
    }
}

public sealed class ChangeBranchStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("branches/{id:guid}/activate", (Guid id, ChangeBranchStatusHandler handler, CancellationToken ct) =>
                Handle(handler, id, activate: true, ct))
            .WithName("ActivateBranch")
            .WithTags("Branches")
            .Produces<BranchResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPost("branches/{id:guid}/deactivate", (Guid id, ChangeBranchStatusHandler handler, CancellationToken ct) =>
                Handle(handler, id, activate: false, ct))
            .WithName("DeactivateBranch")
            .WithTags("Branches")
            .Produces<BranchResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(ChangeBranchStatusHandler handler, Guid id, bool activate, CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, activate, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }
}
