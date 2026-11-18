using System.ComponentModel.DataAnnotations;
using System.Net;
using SimpleEcom.UI.Web.Admin.Features.Branches;

namespace SimpleEcom.UI.Web.Admin.Test.Features.Branches;

public class BranchEditorViewModelTests
{
    private readonly FakeApiHandler _api = new();
    private readonly BranchEditorViewModel _vm;

    public BranchEditorViewModelTests() => _vm = new BranchEditorViewModel(_api.CreateBranchesClient());

    [Fact]
    public void StartNew_PreparesAnEmptyFormOfferingPickupAndDelivery()
    {
        _vm.StartNew();

        Assert.False(_vm.IsEdit);
        Assert.True(_vm.IsLoaded);
        Assert.True(_vm.Form.AllowsPickup);
        Assert.True(_vm.Form.AllowsDelivery);
        Assert.Equal("Crear sucursal", _vm.SubmitText);
    }

    [Fact]
    public async Task SaveAsync_NewBranch_PostsAndReturnsTrue()
    {
        _vm.StartNew();
        _vm.Form.Code = "NORTE";
        _vm.Form.Name = "Sucursal Norte";
        _vm.Form.Address = "Calle 1";
        _api.On(HttpMethod.Post, "/api/branches", HttpStatusCode.Created, FakeApiHandler.Branch(code: "NORTE"));

        var saved = await _vm.SaveAsync();

        Assert.True(saved);
        Assert.Null(_vm.Save.Error);
        Assert.False(_vm.Save.IsBusy);
        Assert.Equal(["POST /api/branches"], _api.Requests);
    }

    [Fact]
    public async Task SaveAsync_WhenCodeAlreadyExists_ReturnsFalseWithTheApiMessage()
    {
        _vm.StartNew();
        _api.On(HttpMethod.Post, "/api/branches", _ =>
            FakeApiHandler.Problem(HttpStatusCode.Conflict, "Branch.CodeAlreadyExists", "Ya existe una sucursal con código 'NORTE'."));

        var saved = await _vm.SaveAsync();

        Assert.False(saved);
        Assert.Equal("Ya existe una sucursal con código 'NORTE'.", _vm.Save.Error);
    }

    [Fact]
    public async Task LoadAsync_ExistingBranch_FillsTheForm()
    {
        var branch = FakeApiHandler.Branch(code: "NORTE", name: "Sucursal Norte", allowsDelivery: false, isActive: false);
        _api.On(HttpMethod.Get, $"/api/branches/{branch.Id}", HttpStatusCode.OK, branch);

        var loaded = await _vm.LoadAsync(branch.Id);

        Assert.True(loaded);
        Assert.True(_vm.IsEdit);
        Assert.False(_vm.IsActive);
        Assert.Equal("NORTE", _vm.Form.Code);
        Assert.Equal("Sucursal Norte", _vm.Form.Name);
        Assert.False(_vm.Form.AllowsDelivery);
        Assert.Equal("Guardar cambios", _vm.SubmitText);
    }

    [Fact]
    public async Task LoadAsync_UnknownBranch_ReturnsFalseWithTheApiMessage()
    {
        var id = Guid.NewGuid();
        _api.On(HttpMethod.Get, $"/api/branches/{id}", _ =>
            FakeApiHandler.Problem(HttpStatusCode.NotFound, "Branch.NotFound", "No existe la sucursal."));

        var loaded = await _vm.LoadAsync(id);

        Assert.False(loaded);
        Assert.False(_vm.IsLoaded);
        Assert.Equal("No existe la sucursal.", _vm.Load.Error);
    }

    [Fact]
    public async Task SaveAsync_ExistingBranch_PutsToItsId()
    {
        var branch = FakeApiHandler.Branch();
        _api.On(HttpMethod.Get, $"/api/branches/{branch.Id}", HttpStatusCode.OK, branch);
        _api.On(HttpMethod.Put, $"/api/branches/{branch.Id}", HttpStatusCode.OK, branch);
        await _vm.LoadAsync(branch.Id);

        var saved = await _vm.SaveAsync();

        Assert.True(saved);
        Assert.Equal(1, _api.CountRequests(HttpMethod.Put, $"/api/branches/{branch.Id}"));
    }

    [Fact]
    public void FormValidation_WithoutFulfillmentOptions_ReportsAnError()
    {
        var form = new BranchFormModel
        {
            Code = "NORTE",
            Name = "Norte",
            Address = "Calle 1",
            AllowsPickup = false,
            AllowsDelivery = false
        };

        var errors = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(form, new ValidationContext(form), errors, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(BranchFormModel.AllowsPickup)));
    }
}
