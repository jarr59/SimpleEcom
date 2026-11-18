using SimpleEcom.Api.Features.Branches.ChangeBranchStatus;
using SimpleEcom.Api.Features.Branches.CreateBranch;
using SimpleEcom.Api.Features.Branches.DeleteBranch;
using SimpleEcom.Api.Features.Branches.GetBranchById;
using SimpleEcom.Api.Features.Branches.GetBranches;
using SimpleEcom.Api.Features.Branches.UpdateBranch;
using SimpleEcom.Api.Infrastructure.Persistence;
using SimpleEcom.Shared.Contracts.Branches;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Test.Features.Branches;

public class BranchHandlersTests
{
    private readonly InMemoryBranchRepository _repository = new();

    private static CreateBranchRequest Request(string code, string name = "Sucursal", bool allowsPickup = true, bool allowsDelivery = true) =>
        new(code, name, "Calle 1", null, allowsPickup, allowsDelivery);

    private async Task<BranchResponse> SeedAsync(string code, string name = "Sucursal") =>
        (await new CreateBranchHandler(_repository, TimeProvider.System).HandleAsync(Request(code, name))).Value;

    [Fact]
    public async Task Create_ValidRequest_ReturnsActiveBranchWithNormalizedCode()
    {
        var result = await new CreateBranchHandler(_repository, TimeProvider.System).HandleAsync(Request(" norte "));

        Assert.True(result.IsSuccess);
        Assert.Equal("NORTE", result.Value.Code);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task Create_DuplicateCodeIgnoringCase_ReturnsConflict()
    {
        await SeedAsync("NORTE");

        var result = await new CreateBranchHandler(_repository, TimeProvider.System).HandleAsync(Request("norte"));

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Branch.CodeAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Create_WithoutFulfillmentOptions_ReturnsValidationAndDoesNotPersist()
    {
        var result = await new CreateBranchHandler(_repository, TimeProvider.System)
            .HandleAsync(Request("NORTE", allowsPickup: false, allowsDelivery: false));

        Assert.Equal("Branch.NoFulfillmentOption", result.Error.Code);
        Assert.Empty(await new GetBranchesHandler(_repository).HandleAsync());
    }

    [Fact]
    public async Task GetAll_FiltersBySearchAndStatus()
    {
        await SeedAsync("CENTRO", "Sucursal Centro");
        var norte = await SeedAsync("NORTE", "Sucursal Norte");
        await new ChangeBranchStatusHandler(_repository).HandleAsync(norte.Id, activate: false);

        var handler = new GetBranchesHandler(_repository);

        Assert.Equal(["CENTRO"], (await handler.HandleAsync(search: "centro")).Select(b => b.Code));
        Assert.Equal(["CENTRO"], (await handler.HandleAsync(isActive: true)).Select(b => b.Code));
        Assert.Equal(["NORTE"], (await handler.HandleAsync(isActive: false)).Select(b => b.Code));
    }

    [Fact]
    public async Task Update_ValidRequest_ChangesEditableFieldsButKeepsCode()
    {
        var created = await SeedAsync("CENTRO");

        var result = await new UpdateBranchHandler(_repository)
            .HandleAsync(created.Id, new UpdateBranchRequest("Nuevo nombre", "Calle 9", "555", AllowsPickup: false, AllowsDelivery: true));

        Assert.True(result.IsSuccess);
        Assert.Equal("CENTRO", result.Value.Code);
        Assert.Equal("Nuevo nombre", result.Value.Name);
        Assert.Equal("Calle 9", result.Value.Address);
        Assert.False(result.Value.AllowsPickup);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var result = await new UpdateBranchHandler(_repository)
            .HandleAsync(Guid.NewGuid(), new UpdateBranchRequest("X", "Calle", null, true, true));

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task ChangeStatus_Deactivate_PersistsInactiveState()
    {
        var created = await SeedAsync("CENTRO");

        var result = await new ChangeBranchStatusHandler(_repository).HandleAsync(created.Id, activate: false);

        Assert.False(result.Value.IsActive);
        var stored = await new GetBranchByIdHandler(_repository).HandleAsync(created.Id);
        Assert.False(stored.Value.IsActive);
    }

    [Fact]
    public async Task Delete_ExistingBranch_RemovesIt()
    {
        var created = await SeedAsync("CENTRO");

        var result = await new DeleteBranchHandler(_repository).HandleAsync(created.Id);

        Assert.True(result.IsSuccess);
        var stored = await new GetBranchByIdHandler(_repository).HandleAsync(created.Id);
        Assert.Equal(ErrorType.NotFound, stored.Error.Type);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var result = await new DeleteBranchHandler(_repository).HandleAsync(Guid.NewGuid());

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
