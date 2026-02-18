namespace Bam.Svc;

public class PersonRegistrationRequest
{
    public string? Handle { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
