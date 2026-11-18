using SimpleEcom.Api.Common.Endpoints;
using SimpleEcom.Api.Common.Http;
using SimpleEcom.Api.Domain.Branches;
using SimpleEcom.Shared.Contracts.Branches;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Features.Branches.CreateBranch;

public sealed class CreateBranchHandler(IBranchRepository repository, TimeProvider timeProvider)
{
    public async Task<Result<BranchResponse>> HandleAsync(CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var branch = Branch.Create(
            request.Code,
            request.Name,
            request.Address,
            request.Phone,
            request.AllowsPickup,
            request.AllowsDelivery,
            timeProvider);

        if (branch.IsFailure)
        {
            return branch.Error;
        }

        if (await repository.CodeExistsAsync(branch.Value.Code, cancellationToken))
        {
            return BranchErrors.CodeAlreadyExists(branch.Value.Code);
        }

        await repository.AddAsync(branch.Value, cancellationToken);
        return branch.Value.ToResponse();
    }
}

public sealed class CreateBranchEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("branches", async (CreateBranchRequest request, CreateBranchHandler handler, CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/branches/{result.Value.Id}", result.Value)
                    : result.ToProblem();
            })
            .WithName("CreateBranch")
            .WithTags("Branches")
            .Produces<BranchResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
}
