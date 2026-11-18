using SimpleEcom.Shared.Contracts;
using SimpleEcom.UI.Web.Features.Catalog;

namespace SimpleEcom.UI.Web.Test.Features.Catalog;

public class CatalogDisplayTests
{
    [Theory]
    [InlineData(0, StockStatus.OutOfStock, "Agotado")]
    [InlineData(1, StockStatus.LowStock, "¡Última unidad!")]
    [InlineData(5, StockStatus.LowStock, "¡Últimas 5 unidades!")]
    [InlineData(6, StockStatus.InStock, "En stock")]
    public void StockLabel_DependsOnTheStock(int stock, StockStatus expectedStatus, string expectedLabel)
    {
        Assert.Equal(expectedStatus, CatalogDisplay.GetStockStatus(stock));
        Assert.Equal(expectedLabel, CatalogDisplay.StockLabel(stock));
    }

    [Fact]
    public void FormatPrice_UsesTheCurrencyConvention()
    {
        Assert.Equal("$1,234.50", CatalogDisplay.FormatPrice(1234.5m, Currency.USD));
        Assert.Equal("1234,50 €", CatalogDisplay.FormatPrice(1234.5m, Currency.EUR).Replace(' ', ' ').Replace(".", ""));
    }

    [Theory]
    [InlineData("Camiseta básica", "CB")]
    [InlineData("taza", "T")]
    [InlineData("Pack de calcetines largos", "PC")]
    [InlineData("Bolsa de tela", "BT")]
    [InlineData("  ", "?")]
    public void Initials_TakesUpToTwoLetters(string name, string expected) =>
        Assert.Equal(expected, CatalogDisplay.Initials(name));

    [Fact]
    public void Hue_IsStableAndInRange()
    {
        var hue = CatalogDisplay.Hue("TSHIRT-001");

        Assert.Equal(hue, CatalogDisplay.Hue("TSHIRT-001"));
        Assert.InRange(hue, 0, 359);
    }
}
