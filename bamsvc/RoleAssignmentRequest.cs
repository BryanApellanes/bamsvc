namespace Bam.Svc;

public class RoleAssignmentRequest
{
    public string PersonHandle { get; set; } = null!;
    public string[] RoleNames { get; set; } = [];
}
