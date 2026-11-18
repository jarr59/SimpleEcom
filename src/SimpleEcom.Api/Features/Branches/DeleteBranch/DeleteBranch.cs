using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Common.Http;
using SimpleEcom.Api.Domain.Branches;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Features.Branches.DeleteBranch;

/// <summary>
/// Elimina una sucursal. Cuando existan precios, stock o pedidos asociados (fases siguientes)
/// se deberá impedir el borrado y desactivarla en su lugar.
/// </summary>
public sealed class DeleteBranchHandler(IBranchRepository repository)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var branchId = new BranchId(id);
        var branch = await repository.GetByIdAsync(branchId, cancellationToken);
        if (branch is null)
        {
            return Result.Failure(BranchErrors.NotFound(branchId));
        }

        await repository.RemoveAsync(branch, cancellationToken);
        return Result.Success();
    }
}

public sealed class DeleteBranchEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("branches/{id:guid}", async (Guid id, DeleteBranchHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .WithName("DeleteBranch")
            .WithTags("Branches")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
}
