using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Domain.Branches;
using SimpleEcom.Shared.Contracts.Branches;

namespace SimpleEcom.Api.Features.Branches.GetBranches;

public sealed class GetBranchesHandler(IBranchRepository repository)
{
    /// <param name="search">Texto a buscar en nombre o código (opcional).</param>
    /// <param name="isActive">Filtra por estado; null devuelve todas.</param>
    public async Task<IReadOnlyList<BranchResponse>> HandleAsync(string? search = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var branches = await repository.GetAllAsync(cancellationToken);

        var query = branches.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b =>
                b.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                b.Code.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive is not null)
        {
            query = query.Where(b => b.IsActive == isActive);
        }

        return query.Select(b => b.ToResponse()).ToList();
    }
}

public sealed class GetBranchesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("branches", async (string? search, bool? isActive, GetBranchesHandler handler, CancellationToken ct) =>
                Results.Ok(await handler.HandleAsync(search, isActive, ct)))
            .WithName("GetBranches")
            .WithTags("Branches")
            .Produces<IReadOnlyList<BranchResponse>>();
}
