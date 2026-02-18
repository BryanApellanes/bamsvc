using System.Reflection;
using System.Text;

namespace Bam.Presentation;

public class HtmlPage : IHtmlPage
{
    public HtmlPage(string path, string title, string bodyHtml)
    {
        Path = path;
        Title = title;
        BodyHtml = bodyHtml;
    }

    public string Path { get; }
    public string Title { get; }
    protected string BodyHtml { get; }

    public virtual string LayoutHtml =>
        """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>{Title}</title>
            <style>
                body { font-family: system-ui, -apple-system, sans-serif; max-width: 640px; margin: 2rem auto; padding: 0 1rem; color: #222; }
                h1 { font-size: 1.5rem; }
                a { color: #0066cc; }
                label { display: block; margin-top: 0.75rem; font-weight: 500; }
                input { display: block; width: 100%; padding: 0.4rem; margin-top: 0.25rem; box-sizing: border-box; border: 1px solid #ccc; border-radius: 3px; }
                button { margin-top: 1rem; padding: 0.5rem 1.5rem; background: #0066cc; color: #fff; border: none; border-radius: 3px; cursor: pointer; }
                button:hover { background: #0052a3; }
                .message { margin-top: 1rem; padding: 0.75rem; border-radius: 3px; }
                .error { background: #fee; border: 1px solid #c00; color: #c00; }
                .success { background: #efe; border: 1px solid #0a0; color: #060; }
            </style>
        </head>
        <body>
        {Body}
        </body>
        </html>
        """;

    public virtual string Render(object? model = null)
    {
        string html = LayoutHtml
            .Replace("{Title}", Title)
            .Replace("{Body}", BodyHtml);

        if (model != null)
        {
            html = ReplaceTokens(html, model);
        }

        return html;
    }

    private static string ReplaceTokens(string html, object model)
    {
        foreach (PropertyInfo prop in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            string token = $"{{{prop.Name}}}";
            string value = prop.GetValue(model)?.ToString() ?? string.Empty;
            html = html.Replace(token, value);
        }

        return html;
    }
}
