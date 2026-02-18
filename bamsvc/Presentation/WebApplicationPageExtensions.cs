using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Bam.Presentation;

public static class WebApplicationPageExtensions
{
    public static WebApplication MapPage(this WebApplication app, IHtmlPage page)
    {
        app.MapGet(page.Path, () => Results.Content(page.Render(), page.ContentType));
        return app;
    }

    public static WebApplication MapPages(this WebApplication app, params IHtmlPage[] pages)
    {
        foreach (var page in pages)
        {
            app.MapPage(page);
        }

        return app;
    }
}
