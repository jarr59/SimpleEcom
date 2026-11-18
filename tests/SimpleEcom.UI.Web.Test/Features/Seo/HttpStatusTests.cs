using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using SimpleEcom.UI.Web.Features.Seo;

namespace SimpleEcom.UI.Web.Test.Features.Seo;

public class HttpStatusTests
{
    private sealed class StatusCodePagesFeature : IStatusCodePagesFeature
    {
        public bool Enabled { get; set; } = true;
    }

    [Fact]
    public void SetNotFound_Sets404AndKeepsThePageOwnContent()
    {
        var context = new DefaultHttpContext();
        var statusCodePages = new StatusCodePagesFeature();
        context.Features.Set<IStatusCodePagesFeature>(statusCodePages);

        HttpStatus.SetNotFound(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.False(statusCodePages.Enabled, "la página de /not-found no debe sustituir el mensaje del producto");
    }

    [Fact]
    public void SetUnavailable_Sets503()
    {
        var context = new DefaultHttpContext();

        HttpStatus.SetUnavailable(context);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public void WithoutHttpContext_DoesNothing()
    {
        HttpStatus.SetNotFound(null);
        HttpStatus.SetUnavailable(null);
    }
}
