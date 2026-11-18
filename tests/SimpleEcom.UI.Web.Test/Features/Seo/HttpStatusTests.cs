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
    public void SetUnavailable_Sets503AndKeepsThePageOwnContent()
    {
        var context = new DefaultHttpContext();
        var statusCodePages = new StatusCodePagesFeature();
        context.Features.Set<IStatusCodePagesFeature>(statusCodePages);

        HttpStatus.SetUnavailable(context);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.False(statusCodePages.Enabled, "la página de /not-found no debe sustituir el aviso de la página");
    }

    [Fact]
    public void WithoutHttpContext_DoesNothing()
    {
        HttpStatus.SetUnavailable(null);
    }
}
