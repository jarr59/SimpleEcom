using SimpleEcom.Api.Features.Products.CreateProduct;
using SimpleEcom.Api.Features.Products.GetProductById;
using SimpleEcom.Api.Infrastructure.Persistence;
using SimpleEcom.Shared.Contracts;
using SimpleEcom.Shared.Contracts.Products;
using SimpleEcom.Shared.Results;

namespace SimpleEcom.Api.Test.Features.Products;

public class CreateProductHandlerTests
{
    private readonly InMemoryProductRepository _repository = new();
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerTests() => _handler = new CreateProductHandler(_repository, TimeProvider.System);

    private static CreateProductRequest Request(string sku = "TSHIRT-001", decimal price = 19.99m) =>
        new(sku, "Camiseta", "Algodón", price, Currency.USD, 10);

    [Fact]
    public async Task HandleAsync_ValidRequest_PersistsProduct()
    {
        var result = await _handler.HandleAsync(Request());

        Assert.True(result.IsSuccess);
        var stored = await new GetProductByIdHandler(_repository).HandleAsync(result.Value.Id);
        Assert.Equal("TSHIRT-001", stored.Value.Sku);
    }

    [Fact]
    public async Task HandleAsync_DuplicatedSku_ReturnsConflict()
    {
        await _handler.HandleAsync(Request());

        var result = await _handler.HandleAsync(Request(sku: "tshirt-001"));

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_NegativePrice_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(Request(price: -1m));

        Assert.Equal("Product.NegativePrice", result.Error.Code);
    }
}
