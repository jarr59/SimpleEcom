using System.Net;
using SimpleEcom.UI.Web.Features.Branches;

namespace SimpleEcom.UI.Web.Test.Features.Branches;

public class BranchPickerViewModelTests
{
    private const string ActiveBranches = "/api/branches?isActive=true";

    private readonly FakeApiHandler _api = new();
    private readonly BranchPickerViewModel _vm;

    public BranchPickerViewModelTests() => _vm = new BranchPickerViewModel(_api.CreateBranchesClient());

    [Fact]
    public async Task LoadAsync_PutsTheCurrentBranchFirst()
    {
        var centro = FakeApiHandler.Branch(code: "CENTRO");
        var norte = FakeApiHandler.Branch(code: "NORTE");
        _api.On(HttpMethod.Get, ActiveBranches, HttpStatusCode.OK, new[] { centro, norte });

        _vm.Initialize(norte.Id, returnUrl: "/products", status: null);
        await _vm.LoadAsync();

        Assert.Equal([norte.Id, centro.Id], _vm.Branches!.Select(b => b.Id));
        Assert.True(_vm.IsCurrent(norte.Id));
        Assert.False(_vm.IsCurrent(centro.Id));
        Assert.Equal("/products", _vm.ReturnUrl);
    }

    [Fact]
    public async Task LoadAsync_WhenApiFails_ShowsErrorAndEmptyList()
    {
        _api.On(HttpMethod.Get, ActiveBranches, _ =>
            FakeApiHandler.Problem(HttpStatusCode.InternalServerError, "Boom", "Algo falló."));

        _vm.Initialize(null, null, null);
        await _vm.LoadAsync();

        Assert.NotNull(_vm.ErrorMessage);
        Assert.Empty(_vm.Branches!);
    }

    [Theory]
    [InlineData(BranchPickerViewModel.StatusUnavailable, "Esa sucursal ya no está disponible. Elige otra, por favor.")]
    [InlineData(BranchPickerViewModel.StatusError, "No pudimos guardar tu sucursal. Inténtalo de nuevo en unos minutos.")]
    [InlineData("otro", null)]
    [InlineData(null, null)]
    public void Initialize_TranslatesTheStatusIntoAMessage(string? status, string? expected)
    {
        _vm.Initialize(null, null, status);

        Assert.Equal(expected, _vm.StatusMessage);
    }

    [Fact]
    public void FulfillmentLabels_ListsTheModesTheBranchOffers()
    {
        Assert.Equal(["Retiro en tienda", "Envío a domicilio"],
            BranchPickerViewModel.FulfillmentLabels(FakeApiHandler.Branch()));
        Assert.Equal(["Envío a domicilio"],
            BranchPickerViewModel.FulfillmentLabels(FakeApiHandler.Branch(allowsPickup: false)));
    }

    [Theory]
    [InlineData("+1 (555) 010-0100", "tel:+15550100100")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    [InlineData("sin número", null)]
    public void PhoneHref_KeepsOnlyDigitsAndPlus(string? phone, string? expected)
    {
        Assert.Equal(expected, BranchPickerViewModel.PhoneHref(phone));
    }
}
