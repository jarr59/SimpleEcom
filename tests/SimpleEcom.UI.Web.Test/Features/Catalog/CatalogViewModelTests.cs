using System.Net;
using SimpleEcom.UI.Web.Features.Catalog;

namespace SimpleEcom.UI.Web.Test.Features.Catalog;

public class CatalogViewModelTests
{
    private const string ActiveProducts = "/api/products?isActive=true";

    private readonly FakeApiHandler _api = new();
    private readonly CatalogViewModel _vm;
    private int _notifications;

    public CatalogViewModelTests()
    {
        _vm = new CatalogViewModel(_api.CreateClient());
        _vm.StateChanged += () => _notifications++;
    }

    [Fact]
    public async Task LoadAsync_RequestsOnlyActiveProducts_AndNotifiesThePage()
    {
        _api.On(HttpMethod.Get, ActiveProducts, HttpStatusCode.OK, new[] { FakeApiHandler.Product(sku: "A"), FakeApiHandler.Product(sku: "B") });

        await _vm.LoadAsync();

        Assert.Equal(["GET " + ActiveProducts], _api.Requests);
        Assert.NotNull(_vm.Products);
        Assert.Equal(2, _vm.Products.Count);
        Assert.Equal("2 productos", _vm.Summary);
        Assert.Null(_vm.ErrorMessage);
        Assert.True(_notifications >= 2, "debe notificar al empezar (busy) y al terminar");
    }

    [Fact]
    public async Task LoadAsync_SendsTrimmedSearch_AndUpdatesTitle()
    {
        _vm.Search = " taza ";
        _api.On(HttpMethod.Get, "/api/products?search=taza&isActive=true", HttpStatusCode.OK, Array.Empty<object>());

        await _vm.LoadAsync();

        Assert.Equal(1, _api.CountRequests(HttpMethod.Get, "/api/products?search=taza&isActive=true"));
        Assert.Equal("Resultados para «taza»", _vm.Title);
        Assert.True(_vm.HasFilters);
        Assert.Empty(_vm.Products!);
        Assert.Null(_vm.Summary);
    }

    [Fact]
    public async Task LoadAsync_NeverShowsInactiveProducts_EvenIfTheApiReturnsThem()
    {
        _api.On(HttpMethod.Get, ActiveProducts, HttpStatusCode.OK, new[]
        {
            FakeApiHandler.Product(sku: "ON"),
            FakeApiHandler.Product(sku: "OFF", isActive: false)
        });

        await _vm.LoadAsync();

        Assert.Equal(["ON"], _vm.Products!.Select(p => p.Sku));
    }

    [Fact]
    public async Task LoadAsync_OnlyAvailable_HidesSoldOutProducts()
    {
        _vm.OnlyAvailable = true;
        _api.On(HttpMethod.Get, ActiveProducts, HttpStatusCode.OK, new[]
        {
            FakeApiHandler.Product(sku: "STOCK", stock: 3),
            FakeApiHandler.Product(sku: "EMPTY", stock: 0)
        });

        await _vm.LoadAsync();

        Assert.Equal(["STOCK"], _vm.Products!.Select(p => p.Sku));
    }

    [Fact]
    public async Task LoadAsync_DefaultSort_MovesSoldOutProductsToTheEnd()
    {
        _api.On(HttpMethod.Get, ActiveProducts, HttpStatusCode.OK, new[]
        {
            FakeApiHandler.Product(sku: "EMPTY", stock: 0),
            FakeApiHandler.Product(sku: "A"),
            FakeApiHandler.Product(sku: "B")
        });

        await _vm.LoadAsync();

        Assert.Equal(["A", "B", "EMPTY"], _vm.Products!.Select(p => p.Sku));
    }

    [Theory]
    [InlineData(CatalogSort.PriceAsc, new[] { "CHEAP", "MID", "EXPENSIVE" })]
    [InlineData(CatalogSort.PriceDesc, new[] { "EXPENSIVE", "MID", "CHEAP" })]
    [InlineData(CatalogSort.Name, new[] { "CHEAP", "EXPENSIVE", "MID" })]
    public async Task LoadAsync_AppliesTheSelectedSort(string sort, string[] expectedSkus)
    {
        _vm.Sort = sort;
        _api.On(HttpMethod.Get, ActiveProducts, HttpStatusCode.OK, new[]
        {
            FakeApiHandler.Product(sku: "MID", name: "Camiseta", price: 20m),
            FakeApiHandler.Product(sku: "EXPENSIVE", name: "Bolsa", price: 50m),
            FakeApiHandler.Product(sku: "CHEAP", name: "Abrebotellas", price: 5m)
        });

        await _vm.LoadAsync();

        Assert.Equal(expectedSkus, _vm.Products!.Select(p => p.Sku));
    }

    [Fact]
    public async Task LoadAsync_UnknownSortFromTheUrl_FallsBackToDefault()
    {
        _vm.Sort = "hack";
        _api.On(HttpMethod.Get, ActiveProducts, HttpStatusCode.OK, Array.Empty<object>());

        await _vm.LoadAsync();

        Assert.Equal(CatalogSort.Default, _vm.Sort);
    }

    [Fact]
    public async Task LoadAsync_WhenTheApiFails_ExposesTheErrorAndAnEmptyList()
    {
        _api.On(HttpMethod.Get, ActiveProducts, _ =>
            FakeApiHandler.Problem(HttpStatusCode.InternalServerError, "Server.Error", "Fallo inesperado."));

        await _vm.LoadAsync();

        Assert.Equal("Fallo inesperado.", _vm.ErrorMessage);
        Assert.NotNull(_vm.Products);
        Assert.Empty(_vm.Products);
        Assert.False(_vm.Load.IsBusy);
    }
}
