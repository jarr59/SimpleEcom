using System.Net;
using SimpleEcom.UI.Web.Features.Branches;

namespace SimpleEcom.UI.Web.Test.Features.Branches;

public class CurrentBranchViewModelTests
{
    private const string ActiveBranches = "/api/branches?isActive=true";

    private readonly FakeApiHandler _api = new();
    private readonly CurrentBranchViewModel _vm;

    public CurrentBranchViewModelTests() => _vm = new CurrentBranchViewModel(_api.CreateBranchesClient());

    [Fact]
    public async Task LoadAsync_WithSelectedBranch_ShowsItAndAllowsChanging()
    {
        var centro = FakeApiHandler.Branch(code: "CENTRO");
        var norte = FakeApiHandler.Branch(code: "NORTE");
        _api.On(HttpMethod.Get, ActiveBranches, HttpStatusCode.OK, new[] { centro, norte });

        await _vm.LoadAsync(norte.Id);

        Assert.Equal(["GET " + ActiveBranches], _api.Requests);
        Assert.Equal(norte.Id, _vm.Current?.Id);
        Assert.True(_vm.IsVisible);
        Assert.True(_vm.CanChange);
        Assert.False(_vm.NeedsSelection);
    }

    [Fact]
    public async Task LoadAsync_WithoutSelectionAndSeveralBranches_AsksTheCustomerToChoose()
    {
        _api.On(HttpMethod.Get, ActiveBranches, HttpStatusCode.OK,
            new[] { FakeApiHandler.Branch(code: "A"), FakeApiHandler.Branch(code: "B") });

        await _vm.LoadAsync(selectedBranchId: null);

        Assert.Null(_vm.Current);
        Assert.True(_vm.NeedsSelection);
    }

    [Fact]
    public async Task LoadAsync_WithOnlyOneActiveBranch_SelectsItWithoutAsking()
    {
        var unica = FakeApiHandler.Branch(code: "UNICA");
        _api.On(HttpMethod.Get, ActiveBranches, HttpStatusCode.OK, new[] { unica });

        await _vm.LoadAsync(selectedBranchId: null);

        Assert.Equal(unica.Id, _vm.Current?.Id);
        Assert.False(_vm.NeedsSelection);
        Assert.False(_vm.CanChange);
    }

    [Fact]
    public async Task LoadAsync_SelectedBranchNoLongerActive_IsTreatedAsNotChosen()
    {
        _api.On(HttpMethod.Get, ActiveBranches, HttpStatusCode.OK,
            new[] { FakeApiHandler.Branch(code: "A"), FakeApiHandler.Branch(code: "B") });

        await _vm.LoadAsync(Guid.NewGuid());

        Assert.Null(_vm.Current);
        Assert.True(_vm.NeedsSelection);
    }

    [Fact]
    public async Task LoadAsync_IgnoresInactiveBranchesEvenIfTheApiReturnsThem()
    {
        var inactive = FakeApiHandler.Branch(code: "OFF", isActive: false);
        var active = FakeApiHandler.Branch(code: "ON");
        _api.On(HttpMethod.Get, ActiveBranches, HttpStatusCode.OK, new[] { inactive, active });

        await _vm.LoadAsync(inactive.Id);

        Assert.Equal(active.Id, _vm.Current?.Id);
        Assert.Single(_vm.Branches);
    }

    [Fact]
    public async Task LoadAsync_WhenApiFails_HidesTheBar()
    {
        _api.On(HttpMethod.Get, ActiveBranches, _ =>
            FakeApiHandler.Problem(HttpStatusCode.InternalServerError, "Boom", "Algo falló."));

        await _vm.LoadAsync(Guid.NewGuid());

        Assert.True(_vm.IsLoaded);
        Assert.False(_vm.IsVisible);
        Assert.False(_vm.NeedsSelection);
    }

    [Fact]
    public async Task LoadAsync_WithoutActiveBranches_HidesTheBar()
    {
        _api.On(HttpMethod.Get, ActiveBranches, HttpStatusCode.OK, Array.Empty<object>());

        await _vm.LoadAsync(selectedBranchId: null);

        Assert.False(_vm.IsVisible);
        Assert.False(_vm.NeedsSelection);
    }
}
