using Bam.Presentation;

namespace Bam.Svc.Pages;

public class IndexPage : HtmlPage
{
    public IndexPage() : base("/", "bamsvc",
        """
        <h1>bamsvc</h1>
        <ul>
            <li><a href="/register">Register</a></li>
        </ul>
        """)
    {
    }
}
