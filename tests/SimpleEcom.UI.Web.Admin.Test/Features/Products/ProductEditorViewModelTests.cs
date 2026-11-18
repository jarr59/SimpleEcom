using System.Net;
using SimpleEcom.Shared.Contracts;
using SimpleEcom.UI.Web.Admin.Features.Products;

namespace SimpleEcom.UI.Web.Admin.Test.Features.Products;

public class ProductEditorViewModelTests
{
    private readonly FakeApiHandler _api = new();
    private readonly ProductEditorViewModel _vm;

    public ProductEditorViewModelTests() => _vm = new ProductEditorViewModel(_api.CreateClient());

    [Fact]
    public void StartNew_PreparesAnEmptyFormInUsd()
    {
        _vm.StartNew();

        Assert.False(_vm.IsEdit);
        Assert.True(_vm.IsLoaded);
        Assert.Equal(Currency.USD, _vm.Form.Currency);
        Assert.Equal("Crear producto", _vm.SubmitText);
    }

    [Fact]
    public async Task SaveAsync_NewProduct_PostsAndReturnsTrue()
    {
        _vm.StartNew();
        _vm.Form.Sku = "NEW-1";
        _vm.Form.Name = "Nuevo";
        _api.On(HttpMethod.Post, "/api/products", HttpStatusCode.Created, FakeApiHandler.Product(sku: "NEW-1"));

        var saved = await _vm.SaveAsync();

        Assert.True(saved);
        Assert.Null(_vm.Save.Error);
        Assert.False(_vm.Save.IsBusy);
        Assert.Equal(["POST /api/products"], _api.Requests);
    }

    [Fact]
    public async Task SaveAsync_WhenSkuAlreadyExists_ReturnsFalseWithTheApiMessage()
    {
        _vm.StartNew();
        _api.On(HttpMethod.Post, "/api/products", _ =>
            FakeApiHandler.Problem(HttpStatusCode.Conflict, "Product.SkuAlreadyExists", "Ya existe un producto con SKU 'X'."));

        var saved = await _vm.SaveAsync();

        Assert.False(saved);
        Assert.Equal("Ya existe un producto con SKU 'X'.", _vm.Save.Error);
    }

    [Fact]
    public async Task SaveAsync_IsBusyWhileTheRequestIsInFlight()
    {
        _vm.StartNew();
        var busyDuringCall = false;
        _api.On(HttpMethod.Post, "/api/products", _ =>
        {
            busyDuringCall = _vm.Save.IsBusy;
            return FakeApiHandler.Response(HttpStatusCode.Created, FakeApiHandler.Product());
        });

        await _vm.SaveAsync();

        Assert.True(busyDuringCall);
        Assert.False(_vm.Save.IsBusy);
    }

    [Fact]
    public async Task LoadAsync_ExistingProduct_FillsTheForm()
    {
        var product = FakeApiHandler.Product(sku: "TSHIRT-1", name: "Camiseta", isActive: false);
        _api.On(HttpMethod.Get, $"/api/products/{product.Id}", HttpStatusCode.OK, product);

        var loaded = await _vm.LoadAsync(product.Id);

        Assert.True(loaded);
        Assert.True(_vm.IsEdit);
        Assert.True(_vm.IsLoaded);
        Assert.False(_vm.IsActive);
        Assert.Equal("TSHIRT-1", _vm.Form.Sku);
        Assert.Equal("Camiseta", _vm.Form.Name);
        Assert.Equal("Guardar cambios", _vm.SubmitText);
    }

    [Fact]
    public async Task LoadAsync_UnknownProduct_ReturnsFalseWithError()
    {
        var id = Guid.NewGuid();
        _api.On(HttpMethod.Get, $"/api/products/{id}", _ =>
            FakeApiHandler.Problem(HttpStatusCode.NotFound, "Product.NotFound", "No existe el producto."));

        var loaded = await _vm.LoadAsync(id);

        Assert.False(loaded);
        Assert.False(_vm.IsLoaded);
        Assert.Equal("No existe el producto.", _vm.Load.Error);
    }

    [Fact]
    public async Task SaveAsync_InEditMode_PutsToTheProductUrl()
    {
        var product = FakeApiHandler.Product();
        _api.On(HttpMethod.Get, $"/api/products/{product.Id}", HttpStatusCode.OK, product)
            .On(HttpMethod.Put, $"/api/products/{product.Id}", HttpStatusCode.OK, product with { Name = "Editado" });
        await _vm.LoadAsync(product.Id);

        var saved = await _vm.SaveAsync();

        Assert.True(saved);
        Assert.Equal(1, _api.CountRequests(HttpMethod.Put, $"/api/products/{product.Id}"));
        Assert.Equal(0, _api.CountRequests(HttpMethod.Post, "/api/products"));
    }
}
