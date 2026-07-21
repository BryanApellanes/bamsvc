namespace Bam.Svc;

public class AgentRegistrationRequest
{
    public string Name { get; set; } = null!;
    public string? Handle { get; set; }
    public string PersonHandle { get; set; } = null!;
    public string DeviceHandle { get; set; } = null!;
}
