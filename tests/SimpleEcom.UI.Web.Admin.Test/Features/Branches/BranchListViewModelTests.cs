using System.Net;
using SimpleEcom.UI.Web.Admin.Features.Branches;

namespace SimpleEcom.UI.Web.Admin.Test.Features.Branches;

public class BranchListViewModelTests
{
    private readonly FakeApiHandler _api = new();
    private readonly BranchListViewModel _vm;

    public BranchListViewModelTests() => _vm = new BranchListViewModel(_api.CreateBranchesClient());

    [Fact]
    public async Task LoadAsync_PopulatesBranches()
    {
        _api.On(HttpMethod.Get, "/api/branches", HttpStatusCode.OK,
            new[] { FakeApiHandler.Branch(code: "A"), FakeApiHandler.Branch(code: "B") });

        await _vm.LoadAsync();

        Assert.NotNull(_vm.Branches);
        Assert.Equal(2, _vm.Branches.Count);
        Assert.False(_vm.Load.IsBusy);
        Assert.Null(_vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_SendsSearchAndStatusFilters()
    {
        _vm.Search = " norte ";
        _vm.StatusFilter = "false";
        _api.On(HttpMethod.Get, "/api/branches?search=norte&isActive=false", HttpStatusCode.OK, Array.Empty<object>());

        await _vm.LoadAsync();

        Assert.Equal(1, _api.CountRequests(HttpMethod.Get, "/api/branches?search=norte&isActive=false"));
        Assert.Null(_vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenApiFails_ShowsErrorAndEmptyList()
    {
        _api.On(HttpMethod.Get, "/api/branches", _ =>
            FakeApiHandler.Problem(HttpStatusCode.InternalServerError, "Boom", "Algo falló."));

        await _vm.LoadAsync();

        Assert.Equal("Algo falló.", _vm.ErrorMessage);
        Assert.NotNull(_vm.Branches);
        Assert.Empty(_vm.Branches);
    }

    [Fact]
    public async Task ToggleActiveAsync_DeactivatesShowsMessageAndReloads()
    {
        var branch = FakeApiHandler.Branch(name: "Norte", isActive: true);
        var busyDuringCall = false;
        _api.On(HttpMethod.Get, "/api/branches", HttpStatusCode.OK, new[] { branch });
        _api.On(HttpMethod.Post, $"/api/branches/{branch.Id}/deactivate", _ =>
        {
            busyDuringCall = _vm.IsRowBusy(branch.Id, BranchRowAction.Toggle);
            return FakeApiHandler.Response(HttpStatusCode.OK, branch with { IsActive = false });
        });

        await _vm.ToggleActiveAsync(branch);

        Assert.True(busyDuringCall);
        Assert.False(_vm.IsAnyBusy);
        Assert.Equal("«Norte» desactivada.", _vm.SuccessMessage);
        Assert.Equal(1, _api.CountRequests(HttpMethod.Get, "/api/branches"));
    }

    [Fact]
    public async Task ConfirmDeleteAsync_DeletesClosesDialogAndReloads()
    {
        var branch = FakeApiHandler.Branch(name: "Norte");
        _api.On(HttpMethod.Delete, $"/api/branches/{branch.Id}", HttpStatusCode.NoContent);
        _api.On(HttpMethod.Get, "/api/branches", HttpStatusCode.OK, Array.Empty<object>());

        _vm.RequestDelete(branch);
        await _vm.ConfirmDeleteAsync();

        Assert.Null(_vm.BranchToDelete);
        Assert.Equal("«Norte» eliminada.", _vm.SuccessMessage);
        Assert.Equal(1, _api.CountRequests(HttpMethod.Delete, $"/api/branches/{branch.Id}"));
    }

    [Fact]
    public async Task ConfirmDeleteAsync_WhenApiFails_ShowsErrorAndClosesDialog()
    {
        var branch = FakeApiHandler.Branch();
        _api.On(HttpMethod.Delete, $"/api/branches/{branch.Id}", _ =>
            FakeApiHandler.Problem(HttpStatusCode.NotFound, "Branch.NotFound", "No existe la sucursal."));

        _vm.RequestDelete(branch);
        await _vm.ConfirmDeleteAsync();

        Assert.Null(_vm.BranchToDelete);
        Assert.Equal("No existe la sucursal.", _vm.ErrorMessage);
        Assert.Null(_vm.SuccessMessage);
    }

    [Theory]
    [InlineData(true, true, "Retiro y envío")]
    [InlineData(true, false, "Solo retiro")]
    [InlineData(false, true, "Solo envío")]
    public void FulfillmentText_DescribesTheDeliveryModes(bool allowsPickup, bool allowsDelivery, string expected)
    {
        var branch = FakeApiHandler.Branch(allowsPickup: allowsPickup, allowsDelivery: allowsDelivery);

        Assert.Equal(expected, BranchListViewModel.FulfillmentText(branch));
    }
}
