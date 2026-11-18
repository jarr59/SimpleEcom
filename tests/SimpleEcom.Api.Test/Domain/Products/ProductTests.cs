using SimpleEcom.Api.Domain.Products;
using SimpleEcom.Api.Domain.Products.Events;
using SimpleEcom.Shared.Contracts;

namespace SimpleEcom.Api.Test.Domain.Products;

public class ProductTests
{
    private static Money Price(decimal amount = 10m) => Money.Create(amount, Currency.USD).Value;

    [Fact]
    public void Create_WithValidData_NormalizesSkuAndRaisesDomainEvent()
    {
        var result = Product.Create(" tshirt-001 ", "Camiseta", null, Price(), 5, TimeProvider.System);

        Assert.True(result.IsSuccess);
        Assert.Equal("TSHIRT-001", result.Value.Sku);
        Assert.IsType<ProductCreatedDomainEvent>(Assert.Single(result.Value.DomainEvents));
    }

    [Theory]
    [InlineData("", "Camiseta", 1, "Product.SkuRequired")]
    [InlineData("SKU", " ", 1, "Product.NameRequired")]
    [InlineData("SKU", "Camiseta", -1, "Product.NegativeStock")]
    public void Create_WithInvalidData_ReturnsValidationError(string sku, string name, int stock, string expectedCode)
    {
        var result = Product.Create(sku, name, null, Price(), stock, TimeProvider.System);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public void DecreaseStock_MoreThanAvailable_ReturnsInsufficientStock()
    {
        var product = Product.Create("SKU", "Camiseta", null, Price(), 2, TimeProvider.System).Value;

        var result = product.DecreaseStock(3);

        Assert.Equal(ProductErrors.InsufficientStock, result.Error);
        Assert.Equal(2, product.Stock);
    }

    [Fact]
    public void Money_WithSameAmountAndCurrency_AreEqual()
    {
        Assert.Equal(Money.Create(9.99m, Currency.USD).Value, Money.Create(9.990m, Currency.USD).Value);
        Assert.NotEqual(Money.Create(9.99m, Currency.USD).Value, Money.Create(9.99m, Currency.EUR).Value);
    }

    [Fact]
    public void Money_WithUndefinedCurrency_ReturnsInvalidCurrency()
    {
        var result = Money.Create(1m, (Currency)99);

        Assert.Equal(ProductErrors.InvalidCurrency, result.Error);
    }
}
