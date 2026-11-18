using System.Net;
using SimpleEcom.UI.Web.Admin.Features.Products;

namespace SimpleEcom.UI.Web.Admin.Test.Features.Products;

public class ProductListViewModelTests
{
    private readonly FakeApiHandler _api = new();
    private readonly ProductListViewModel _vm;
    private int _notifications;

    public ProductListViewModelTests()
    {
        _vm = new ProductListViewModel(_api.CreateClient());
        _vm.StateChanged += () => _notifications++;
    }

    [Fact]
    public async Task LoadAsync_PopulatesProducts_AndNotifiesThePage()
    {
        _api.On(HttpMethod.Get, "/api/products", HttpStatusCode.OK, new[] { FakeApiHandler.Product(sku: "A"), FakeApiHandler.Product(sku: "B") });

        await _vm.LoadAsync();

        Assert.NotNull(_vm.Products);
        Assert.Equal(2, _vm.Products.Count);
        Assert.False(_vm.Load.IsBusy);
        Assert.Null(_vm.ErrorMessage);
        Assert.True(_notifications >= 2, "debe notificar al empezar (busy) y al terminar");
    }

    [Fact]
    public async Task LoadAsync_IsBusyWhileTheRequestIsInFlight()
    {
        var busyDuringCall = false;
        _api.On(HttpMethod.Get, "/api/products", _ =>
        {
            busyDuringCall = _vm.Load.IsBusy && _vm.IsAnyBusy;
            return FakeApiHandler.Response(HttpStatusCode.OK, Array.Empty<object>());
        });

        await _vm.LoadAsync();

        Assert.True(busyDuringCall);
        Assert.False(_vm.IsAnyBusy);
    }

    [Fact]
    public async Task LoadAsync_SendsSearchAndStatusFilters()
    {
        _vm.Search = " camiseta ";
        _vm.StatusFilter = "true";
        _api.On(HttpMethod.Get, "/api/products?search=camiseta&isActive=true", HttpStatusCode.OK, Array.Empty<object>());

        await _vm.LoadAsync();

        Assert.Equal(1, _api.CountRequests(HttpMethod.Get, "/api/products?search=camiseta&isActive=true"));
        Assert.Null(_vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenApiFails_ShowsErrorAndEmptyList()
    {
        _api.On(HttpMethod.Get, "/api/products", _ => FakeApiHandler.Problem(HttpStatusCode.InternalServerError, "Boom", "Fallo interno."));

        await _vm.LoadAsync();

        Assert.Equal("Fallo interno.", _vm.ErrorMessage);
        Assert.Empty(_vm.Products!);
    }

    [Fact]
    public async Task ToggleActiveAsync_DeactivatesAndReloads()
    {
        var product = FakeApiHandler.Product(isActive: true);
        _api.On(HttpMethod.Post, $"/api/products/{product.Id}/deactivate", HttpStatusCode.OK, product with { IsActive = false })
            .On(HttpMethod.Get, "/api/products", HttpStatusCode.OK, new[] { product with { IsActive = false } });

        await _vm.ToggleActiveAsync(product);

        Assert.Equal([$"POST /api/products/{product.Id}/deactivate", "GET /api/products"], _api.Requests);
        Assert.Contains("desactivado", _vm.SuccessMessage);
        Assert.Null(_vm.BusyProductId);
        Assert.False(_vm.IsAnyBusy);
    }

    [Fact]
    public async Task ToggleActiveAsync_MarksOnlyThatRowAsBusy()
    {
        var product = FakeApiHandler.Product();
        var other = FakeApiHandler.Product();
        var (rowBusy, otherBusy) = (false, false);
        _api.On(HttpMethod.Post, $"/api/products/{product.Id}/deactivate", _ =>
            {
                rowBusy = _vm.IsRowBusy(product.Id, ProductRowAction.Toggle);
                otherBusy = _vm.IsRowBusy(other.Id, ProductRowAction.Toggle);
                return FakeApiHandler.Response(HttpStatusCode.OK, product with { IsActive = false });
            })
            .On(HttpMethod.Get, "/api/products", HttpStatusCode.OK, Array.Empty<object>());

        await _vm.ToggleActiveAsync(product);

        Assert.True(rowBusy);
        Assert.False(otherBusy);
    }

    [Fact]
    public async Task ToggleActiveAsync_WhenApiFails_ShowsApiMessageAndDoesNotReload()
    {
        var product = FakeApiHandler.Product();
        _api.On(HttpMethod.Post, $"/api/products/{product.Id}/deactivate", _ =>
            FakeApiHandler.Problem(HttpStatusCode.NotFound, "Product.NotFound", "No existe el producto."));

        await _vm.ToggleActiveAsync(product);

        Assert.Equal("No existe el producto.", _vm.ErrorMessage);
        Assert.Null(_vm.SuccessMessage);
        Assert.Equal(0, _api.CountRequests(HttpMethod.Get, "/api/products"));
    }

    [Fact]
    public async Task ConfirmDeleteAsync_DeletesClosesModalAndReloads()
    {
        var product = FakeApiHandler.Product(name: "Taza");
        _api.On(HttpMethod.Delete, $"/api/products/{product.Id}", HttpStatusCode.NoContent)
            .On(HttpMethod.Get, "/api/products", HttpStatusCode.OK, Array.Empty<object>());

        _vm.RequestDelete(product);
        Assert.Same(product, _vm.ProductToDelete);

        await _vm.ConfirmDeleteAsync();

        Assert.Null(_vm.ProductToDelete);
        Assert.Contains("Taza", _vm.SuccessMessage);
        Assert.Equal(1, _api.CountRequests(HttpMethod.Delete, $"/api/products/{product.Id}"));
        Assert.Empty(_vm.Products!);
    }

    [Fact]
    public async Task ConfirmDeleteAsync_WithoutSelection_DoesNothing()
    {
        await _vm.ConfirmDeleteAsync();

        Assert.Empty(_api.Requests);
    }

    [Fact]
    public async Task DismissError_ClearsTheMessage()
    {
        _api.On(HttpMethod.Get, "/api/products", _ => FakeApiHandler.Problem(HttpStatusCode.InternalServerError, "Boom", "Fallo."));
        await _vm.LoadAsync();
        Assert.NotNull(_vm.ErrorMessage);

        _vm.DismissError();

        Assert.Null(_vm.ErrorMessage);
    }

    [Theory]
    [InlineData("created", "creado")]
    [InlineData("updated", "actualizado")]
    [InlineData(null, null)]
    public void ShowStatus_MapsQueryParameterToMessage(string? status, string? expectedFragment)
    {
        _vm.ShowStatus(status);

        if (expectedFragment is null)
        {
            Assert.Null(_vm.SuccessMessage);
        }
        else
        {
            Assert.Contains(expectedFragment, _vm.SuccessMessage);
        }
    }
}
