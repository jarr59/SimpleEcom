using System.Net;
using SimpleEcom.UI.Web.Features.Catalog;

namespace SimpleEcom.UI.Web.Test.Features.Catalog;

public class ProductDetailViewModelTests
{
    private const string ActiveProducts = "/api/products?isActive=true";

    private readonly FakeApiHandler _api = new();
    private readonly ProductDetailViewModel _vm;

    public ProductDetailViewModelTests() => _vm = new ProductDetailViewModel(_api.CreateClient());

    [Fact]
    public async Task LoadAsync_ActiveProduct_IsShown_WithRelatedProductsExcludingItself()
    {
        var product = FakeApiHandler.Product(sku: "MAIN");
        _api.On(HttpMethod.Get, $"/api/products/{product.Id}", HttpStatusCode.OK, product)
            .On(HttpMethod.Get, ActiveProducts, HttpStatusCode.OK, new[]
            {
                product,
                FakeApiHandler.Product(sku: "EMPTY", stock: 0),
                FakeApiHandler.Product(sku: "OTHER")
            });

        await _vm.LoadAsync(product.Id);

        Assert.Equal(product, _vm.Product);
        Assert.False(_vm.IsNotFound);
        Assert.Null(_vm.ErrorMessage);
        Assert.True(_vm.CanBuy);
        Assert.Equal(["OTHER", "EMPTY"], _vm.Related.Select(p => p.Sku));
    }

    [Fact]
    public async Task LoadAsync_InactiveProduct_IsTreatedAsNotFound_AndSkipsRelated()
    {
        var product = FakeApiHandler.Product(isActive: false);
        _api.On(HttpMethod.Get, $"/api/products/{product.Id}", HttpStatusCode.OK, product);

        await _vm.LoadAsync(product.Id);

        Assert.Null(_vm.Product);
        Assert.True(_vm.IsNotFound);
        Assert.Null(_vm.ErrorMessage);
        Assert.Equal(0, _api.CountRequests(HttpMethod.Get, ActiveProducts));
    }

    [Fact]
    public async Task LoadAsync_ApiReturns404_IsNotFound_NotAnError()
    {
        var id = Guid.NewGuid();
        _api.On(HttpMethod.Get, $"/api/products/{id}", _ =>
            FakeApiHandler.Problem(HttpStatusCode.NotFound, "Product.NotFound", "No existe."));

        await _vm.LoadAsync(id);

        Assert.True(_vm.IsNotFound);
        Assert.Null(_vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_ApiFailure_ExposesTheError()
    {
        var id = Guid.NewGuid();
        _api.On(HttpMethod.Get, $"/api/products/{id}", _ =>
            FakeApiHandler.Problem(HttpStatusCode.InternalServerError, "Server.Error", "Fallo inesperado."));

        await _vm.LoadAsync(id);

        Assert.False(_vm.IsNotFound);
        Assert.Equal("Fallo inesperado.", _vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_RelatedFailure_StillShowsTheProduct()
    {
        var product = FakeApiHandler.Product();
        _api.On(HttpMethod.Get, $"/api/products/{product.Id}", HttpStatusCode.OK, product)
            .On(HttpMethod.Get, ActiveProducts, _ =>
                FakeApiHandler.Problem(HttpStatusCode.InternalServerError, "Server.Error", "Fallo."));

        await _vm.LoadAsync(product.Id);

        Assert.Equal(product, _vm.Product);
        Assert.Empty(_vm.Related);
        Assert.Null(_vm.ErrorMessage);
    }

    [Fact]
    public async Task CanBuy_IsFalse_WhenTheProductIsSoldOut()
    {
        var product = FakeApiHandler.Product(stock: 0);
        _api.On(HttpMethod.Get, $"/api/products/{product.Id}", HttpStatusCode.OK, product)
            .On(HttpMethod.Get, ActiveProducts, HttpStatusCode.OK, Array.Empty<object>());

        await _vm.LoadAsync(product.Id);

        Assert.False(_vm.CanBuy);
    }
}
