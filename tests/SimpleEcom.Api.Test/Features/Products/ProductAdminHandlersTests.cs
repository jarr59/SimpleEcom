using SimpleEcom.Api.Features.Products.ChangeProductStatus;
using SimpleEcom.Api.Features.Products.CreateProduct;
using SimpleEcom.Api.Features.Products.DeleteProduct;
using SimpleEcom.Api.Features.Products.GetProductById;
using SimpleEcom.Api.Features.Products.GetProducts;
using SimpleEcom.Api.Features.Products.UpdateProduct;
using SimpleEcom.Api.Infrastructure.Persistence;
using SimpleEcom.Shared.Contracts;
using SimpleEcom.Shared.Contracts.Products;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Test.Features.Products;

public class ProductAdminHandlersTests
{
    private readonly InMemoryProductRepository _repository = new();

    private async Task<ProductResponse> SeedAsync(string sku, string name = "Camiseta", int stock = 10)
    {
        var handler = new CreateProductHandler(_repository, TimeProvider.System);
        var result = await handler.HandleAsync(new CreateProductRequest(sku, name, null, 9.99m, Currency.USD, stock));
        return result.Value;
    }

    [Fact]
    public async Task Update_ValidRequest_ChangesEditableFieldsButKeepsSku()
    {
        var created = await SeedAsync("SKU-1");

        var result = await new UpdateProductHandler(_repository)
            .HandleAsync(created.Id, new UpdateProductRequest("Nuevo nombre", "Desc", 12.5m, Currency.EUR, 3));

        Assert.True(result.IsSuccess);
        Assert.Equal("SKU-1", result.Value.Sku);
        Assert.Equal("Nuevo nombre", result.Value.Name);
        Assert.Equal(12.5m, result.Value.Price);
        Assert.Equal(Currency.EUR, result.Value.Currency);
        Assert.Equal(3, result.Value.Stock);
    }

    [Fact]
    public async Task Update_NegativeStock_FailsWithoutPartialChanges()
    {
        var created = await SeedAsync("SKU-1", name: "Original");

        var result = await new UpdateProductHandler(_repository)
            .HandleAsync(created.Id, new UpdateProductRequest("Cambiado", null, 1m, Currency.USD, -1));

        Assert.Equal("Product.NegativeStock", result.Error.Code);
        var stored = await new GetProductByIdHandler(_repository).HandleAsync(created.Id);
        Assert.Equal("Original", stored.Value.Name);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var result = await new UpdateProductHandler(_repository)
            .HandleAsync(Guid.NewGuid(), new UpdateProductRequest("X", null, 1m, Currency.USD, 1));

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task ChangeStatus_DeactivateThenActivate_TogglesIsActive()
    {
        var created = await SeedAsync("SKU-1");
        var handler = new ChangeProductStatusHandler(_repository);

        var deactivated = await handler.HandleAsync(created.Id, activate: false);
        var activated = await handler.HandleAsync(created.Id, activate: true);

        Assert.False(deactivated.Value.IsActive);
        Assert.True(activated.Value.IsActive);
    }

    [Fact]
    public async Task Delete_ExistingProduct_RemovesIt()
    {
        var created = await SeedAsync("SKU-1");

        var deleted = await new DeleteProductHandler(_repository).HandleAsync(created.Id);
        var lookup = await new GetProductByIdHandler(_repository).HandleAsync(created.Id);

        Assert.True(deleted.IsSuccess);
        Assert.Equal(ErrorType.NotFound, lookup.Error.Type);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var result = await new DeleteProductHandler(_repository).HandleAsync(Guid.NewGuid());

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task GetProducts_FiltersBySearchAndStatus()
    {
        var shirt = await SeedAsync("TSHIRT-1", name: "Camiseta roja");
        await SeedAsync("TSHIRT-2", name: "Camiseta azul");
        await SeedAsync("MUG-1", name: "Taza");
        await new ChangeProductStatusHandler(_repository).HandleAsync(shirt.Id, activate: false);
        var handler = new GetProductsHandler(_repository);

        var bySearch = await handler.HandleAsync(search: "camiseta");
        var bySku = await handler.HandleAsync(search: "mug");
        var inactive = await handler.HandleAsync(isActive: false);
        var activeShirts = await handler.HandleAsync(search: "camiseta", isActive: true);

        Assert.Equal(2, bySearch.Count);
        Assert.Equal("MUG-1", Assert.Single(bySku).Sku);
        Assert.Equal(shirt.Id, Assert.Single(inactive).Id);
        Assert.Equal("TSHIRT-2", Assert.Single(activeShirts).Sku);
    }
}
