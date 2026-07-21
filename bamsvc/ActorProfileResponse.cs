namespace Bam.Svc;

/// <summary>
/// The public projection of an actor's profile returned by <see cref="ActorRegistrationService.GetActor"/>.
/// </summary>
public class ActorProfileResponse
{
    public string ProfileHandle { get; set; } = string.Empty;
    public string PersonHandle { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DeviceHandle { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
