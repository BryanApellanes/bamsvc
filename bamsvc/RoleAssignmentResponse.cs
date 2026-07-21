namespace Bam.Svc;

/// <summary>
/// The result of an assign-roles or remove-roles operation on <see cref="ActorRegistrationService"/>.
/// </summary>
public class RoleAssignmentResponse
{
    public string PersonHandle { get; set; } = string.Empty;
    public string[] RoleNames { get; set; } = Array.Empty<string>();
    public bool Success { get; set; }
}
