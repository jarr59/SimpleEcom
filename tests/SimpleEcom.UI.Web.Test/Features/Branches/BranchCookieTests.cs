using Microsoft.AspNetCore.Http;
using SimpleEcom.UI.Web.Features.Branches;

namespace SimpleEcom.UI.Web.Test.Features.Branches;

public class BranchCookieTests
{
    private static DefaultHttpContext Context(bool https, string? cookieHeader = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = https ? "https" : "http";
        if (cookieHeader is not null)
        {
            context.Request.Headers.Cookie = cookieHeader;
        }

        return context;
    }

    [Fact]
    public void Write_OverHttps_UsesHostPrefixAndSecureAttributes()
    {
        var context = Context(https: true);
        var id = Guid.NewGuid();

        BranchCookie.Write(context.Response, id);

        var header = context.Response.Headers.SetCookie.ToString();
        Assert.StartsWith($"{BranchCookie.SecureName}={id}", header);
        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/", header, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Write_OverHttp_UsesUnprefixedNameWithoutSecure()
    {
        var context = Context(https: false);

        BranchCookie.Write(context.Response, Guid.NewGuid());

        var header = context.Response.Headers.SetCookie.ToString();
        Assert.StartsWith($"{BranchCookie.InsecureName}=", header);
        Assert.DoesNotContain("secure", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_ValidCookieForTheScheme_ReturnsTheId()
    {
        var id = Guid.NewGuid();

        Assert.Equal(id, BranchCookie.Read(Context(https: true, $"{BranchCookie.SecureName}={id}")));
        Assert.Equal(id, BranchCookie.Read(Context(https: false, $"{BranchCookie.InsecureName}={id}")));
    }

    [Fact]
    public void Read_OverHttps_IgnoresTheUnprefixedCookie()
    {
        // Una cookie sin prefijo la podría haber fijado un subdominio o la red: por HTTPS solo vale la __Host-.
        var context = Context(https: true, $"{BranchCookie.InsecureName}={Guid.NewGuid()}");

        Assert.Null(BranchCookie.Read(context));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("__Host-simpleecom-branch=no-es-un-guid")]
    public void Read_MissingOrInvalid_ReturnsNull(string? cookieHeader)
    {
        Assert.Null(BranchCookie.Read(Context(https: true, cookieHeader)));
        Assert.Null(BranchCookie.Read(null));
    }
}
