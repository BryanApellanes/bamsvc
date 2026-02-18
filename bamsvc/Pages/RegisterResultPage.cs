using Bam.Presentation;

namespace Bam.Svc.Pages;

public class RegisterResultPage : HtmlPage
{
    public RegisterResultPage() : base("/register/result", "Registration Result - bamsvc",
        """
        <h1>Registration Complete</h1>
        <p><strong>Name:</strong> {Name}</p>
        <p><strong>Person Handle:</strong> {PersonHandle}</p>
        <p><strong>Profile Handle:</strong> {ProfileHandle}</p>
        <p><a href="/register">Register another</a> | <a href="/">Home</a></p>
        """)
    {
    }
}
