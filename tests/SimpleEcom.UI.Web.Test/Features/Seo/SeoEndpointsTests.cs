using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using SimpleEcom.UI.Web.Features.Seo;

namespace SimpleEcom.UI.Web.Test.Features.Seo;

public class SeoEndpointsTests
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    [Fact]
    public void SitemapXml_ListsAbsoluteUrlsOnceInTheSitemapNamespace()
    {
        var xml = SeoEndpoints.SitemapXml("https://shop.test/", ["", "products", "products", "products/abc"]);

        var doc = XDocument.Parse(xml);
        Assert.Equal(Ns + "urlset", doc.Root!.Name);
        Assert.Equal(
            ["https://shop.test/", "https://shop.test/products", "https://shop.test/products/abc"],
            doc.Root.Elements(Ns + "url").Select(u => u.Element(Ns + "loc")!.Value));
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", xml);
    }

    [Fact]
    public void SitemapXml_EscapesSpecialCharacters()
    {
        var xml = SeoEndpoints.SitemapXml("https://shop.test/", ["products?a=1&b=2"]);

        Assert.Contains("products?a=1&amp;b=2", xml);
        Assert.Equal("https://shop.test/products?a=1&b=2", XDocument.Parse(xml).Descendants(Ns + "loc").Single().Value);
    }

    [Fact]
    public void RobotsTxt_AllowsTheStoreBlocksTheSelectionEndpointAndPointsToTheSitemap()
    {
        var robots = SeoEndpoints.RobotsTxt("https://shop.test/");

        Assert.Contains("User-agent: *", robots);
        Assert.Contains("Disallow: /branches/select", robots);
        Assert.Contains("Sitemap: https://shop.test/sitemap.xml", robots);
    }

    [Fact]
    public void BaseUrl_IncludesSchemeHostAndPathBase()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("shop.test", 7171);
        context.Request.PathBase = "/tienda";

        Assert.Equal("https://shop.test:7171/tienda/", SeoEndpoints.BaseUrl(context.Request));
    }

    [Fact]
    public void StaticPaths_IncludeHomeCatalogAndBranches()
    {
        Assert.Equal(["", "products", "branches"], SeoEndpoints.StaticPaths);
    }
}
