using Bam.Presentation;

namespace Bam.Svc.Pages;

public class RegisterPage : HtmlPage
{
    public RegisterPage() : base("/register", "Register - bamsvc",
        """
        <h1>Register</h1>
        <form id="registerForm">
            <label>First Name <input type="text" name="firstName" required /></label>
            <label>Last Name <input type="text" name="lastName" required /></label>
            <label>Email <input type="email" name="email" /></label>
            <label>Phone <input type="tel" name="phone" /></label>
            <label>Handle (optional) <input type="text" name="handle" /></label>
            <button type="submit">Register</button>
        </form>
        <div id="result"></div>
        <script>
            document.getElementById('registerForm').addEventListener('submit', async (e) => {
                e.preventDefault();
                const form = e.target;
                const data = Object.fromEntries(new FormData(form));
                const resultDiv = document.getElementById('result');
                try {
                    const res = await fetch('/api/register', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(data)
                    });
                    const json = await res.json();
                    if (res.ok) {
                        resultDiv.className = 'message success';
                        resultDiv.innerHTML = 'Registered. Person handle: <strong>' + json.personHandle + '</strong>';
                    } else {
                        resultDiv.className = 'message error';
                        resultDiv.textContent = json.error || 'Registration failed';
                    }
                } catch (err) {
                    resultDiv.className = 'message error';
                    resultDiv.textContent = err.message;
                }
            });
        </script>
        """)
    {
    }
}
