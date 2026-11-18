using System.Text.Json;
using SimpleEcom.UI.Web.Features.Catalog;

namespace SimpleEcom.UI.Web.Test.Features.Catalog;

public class CatalogSeoTests
{
    [Fact]
    public void MetaDescription_ShortText_IsNormalizedAndKept()
    {
        Assert.Equal("Algodón 100 %, corte clásico.", CatalogSeo.MetaDescription("  Algodón 100 %,\n  corte   clásico. ", "x"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void MetaDescription_Empty_UsesFallback(string? text)
    {
        Assert.Equal("Fallback", CatalogSeo.MetaDescription(text, "Fallback"));
    }

    [Fact]
    public void MetaDescription_LongText_IsCutAtAWordBoundaryWithEllipsis()
    {
        var text = string.Join(' ', Enumerable.Repeat("palabra", 40));

        var result = CatalogSeo.MetaDescription(text, "x");

        Assert.True(result.Length <= CatalogSeo.MaxDescriptionLength);
        Assert.EndsWith("palabra…", result);
    }

    [Fact]
    public void ProductDescription_WithoutDescription_BuildsOneFromNameAndSku()
    {
        var product = FakeApiHandler.Product(sku: "CAP-001", name: "Gorra") with { Description = null };

        Assert.Equal("Gorra en SimpleEcom. Ref. CAP-001.", CatalogSeo.ProductDescription(product));
    }

    [Fact]
    public void ProductJsonLd_DescribesTheProductAndItsOffer()
    {
        var product = FakeApiHandler.Product(sku: "MUG-001", name: "Taza", price: 9.5m, stock: 3) with { Description = "Taza de 350 ml" };

        using var json = JsonDocument.Parse(CatalogSeo.ProductJsonLd(product, "https://shop.test/products/1"));
        var root = json.RootElement;

        Assert.Equal("https://schema.org", root.GetProperty("@context").GetString());
        Assert.Equal("Product", root.GetProperty("@type").GetString());
        Assert.Equal("Taza", root.GetProperty("name").GetString());
        Assert.Equal("MUG-001", root.GetProperty("sku").GetString());
        Assert.Equal("Taza de 350 ml", root.GetProperty("description").GetString());
        var offer = root.GetProperty("offers");
        Assert.Equal(9.5m, offer.GetProperty("price").GetDecimal());
        Assert.Equal("USD", offer.GetProperty("priceCurrency").GetString());
        Assert.Equal("https://schema.org/InStock", offer.GetProperty("availability").GetString());
    }

    [Fact]
    public void ProductJsonLd_OutOfStockAndNoDescription()
    {
        var product = FakeApiHandler.Product(stock: 0) with { Description = null };

        using var json = JsonDocument.Parse(CatalogSeo.ProductJsonLd(product, "https://shop.test/p"));

        Assert.False(json.RootElement.TryGetProperty("description", out _));
        Assert.Equal("https://schema.org/OutOfStock", json.RootElement.GetProperty("offers").GetProperty("availability").GetString());
    }

    [Fact]
    public void ProductJsonLd_CannotCloseTheScriptTag()
    {
        // El JSON va dentro de <script>: un nombre malicioso no debe poder cerrarlo e inyectar HTML.
        var product = FakeApiHandler.Product(name: "</script><img src=x onerror=alert(1)>");

        var json = CatalogSeo.ProductJsonLd(product, "https://shop.test/p");

        Assert.DoesNotContain("</script>", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<", json);
        Assert.Equal("</script><img src=x onerror=alert(1)>", JsonDocument.Parse(json).RootElement.GetProperty("name").GetString());
    }
}
