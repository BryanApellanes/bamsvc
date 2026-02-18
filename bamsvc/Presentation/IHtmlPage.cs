namespace Bam.Presentation;

public interface IHtmlPage
{
    string Path { get; }
    string Title { get; }
    string ContentType => "text/html";
    string Render(object? model = null);
}
