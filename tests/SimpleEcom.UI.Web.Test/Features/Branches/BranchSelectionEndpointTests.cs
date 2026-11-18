using SimpleEcom.UI.Web.Features.Branches;

namespace SimpleEcom.UI.Web.Test.Features.Branches;

public class BranchSelectionEndpointTests
{
    [Theory]
    [InlineData("/products", "/products")]
    [InlineData("/products?q=taza&sort=price", "/products?q=taza&sort=price")]
    [InlineData("/", "/")]
    public void SafeReturnUrl_LocalPath_IsKept(string returnUrl, string expected)
    {
        Assert.Equal(expected, BranchSelectionEndpoint.SafeReturnUrl(returnUrl));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("products")]
    [InlineData("https://evil.example")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("/products\r\nSet-Cookie: x=1")]
    public void SafeReturnUrl_NonLocalOrMalformed_GoesHome(string? returnUrl)
    {
        Assert.Equal("~/", BranchSelectionEndpoint.SafeReturnUrl(returnUrl));
    }
}
