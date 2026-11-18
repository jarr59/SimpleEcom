using System.Net;
using SimpleEcom.UI.Web.Features.Catalog;

namespace SimpleEcom.UI.Web.Test.Features.Catalog;

public class FeaturedProductsViewModelTests
{
    private const string ActiveProducts = "/api/products?isActive=true";

    private readonly FakeApiHandler _api = new();
    private readonly FeaturedProductsViewModel _vm;

    public FeaturedProductsViewModelTests() => _vm = new FeaturedProductsViewModel(_api.CreateClient());

    [Fact]
    public async Task LoadAsync_PicksAtMostFourProductsWithStock()
    {
        var products = Enumerable.Range(1, 6)
            .Select(i => FakeApiHandler.Product(sku: $"P{i}", stock: i == 2 ? 0 : 10))
            .ToArray();
        _api.On(HttpMethod.Get, ActiveProducts, HttpStatusCode.OK, products);

        await _vm.LoadAsync();

        Assert.Equal(["P1", "P3", "P4", "P5"], _vm.Products!.Select(p => p.Sku));
    }

    [Fact]
    public async Task LoadAsync_WhenTheApiIsDown_ExposesTheError()
    {
        _api.On(HttpMethod.Get, ActiveProducts, _ => throw new HttpRequestException("caída"));

        await _vm.LoadAsync();

        Assert.NotNull(_vm.ErrorMessage);
        Assert.Empty(_vm.Products!);
    }
}
