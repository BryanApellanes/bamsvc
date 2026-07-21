using Bam.Identity;
using Bam.Protocol;
using Bam.Server;

namespace Bam.Svc;

/// <summary>
/// The pipeline-routed surface for bamsvc's public identity routes (<c>/api/register</c>,
/// <c>/api/profile/{handle}</c>). Registered through <see cref="RouteHandlerRegistrar"/> via
/// <c>WebApplicationBamServer.AddRouteHandler&lt;IdentityGateway&gt;()</c> so every request runs the full
/// BamPipeline (access control, encryption, session management) instead of bypassing it with raw
/// minimal-API mappings. Outcome computation stays in <see cref="IdentityGatewayRoutes"/>.
/// </summary>
[WebService]
[RequiredAccess(BamAccess.Execute)]
[RoutePrefix("/api")]
public class IdentityGateway
{
    private readonly IdentityGatewayRoutes _gatewayRoutes;

    public IdentityGateway(IRegistrationService registrationService)
    {
        _gatewayRoutes = new IdentityGatewayRoutes(registrationService);
    }

    [AnonymousAccess]
    [RoutePath("/register", "POST")]
    public GatewayOutcome RegisterPerson(string firstName, string lastName, string? email, string? phone, string? handle)
    {
        return _gatewayRoutes.BuildRegisterOutcome(new PersonRegistrationRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            Handle = handle,
        });
    }

    [AnonymousAccess]
    [RoutePath("/profile/{handle}", "GET")]
    public GatewayOutcome GetProfile(string handle)
    {
        return _gatewayRoutes.BuildProfileOutcome(handle);
    }
}
