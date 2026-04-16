using Bam.Protocol;
using Bam.Protocol.Profile.Registration;
using Bam.Server;
using Bam.UserAccounts;

namespace Bam.Svc;

[WebService]
[RequiredAccess(BamAccess.Execute)]
[RoutePrefix("/api/actors")]
public class ActorRegistrationService
{
    private readonly IUserAccountService _userAccountService;

    public ActorRegistrationService(IUserAccountService userAccountService)
    {
        _userAccountService = userAccountService;
    }

    [AnonymousAccess(encryptionRequired: true)]
    [RoutePath("/register-agent", "POST")]
    public RegistrationResult RegisterAgent(string name, string personHandle, string deviceHandle, string? handle)
    {
        return _userAccountService.RegisterAgent(new AgentRegistrationData
        {
            Name = name,
            PersonHandle = personHandle,
            DeviceHandle = deviceHandle,
            Handle = handle ?? string.Empty,
        });
    }

    [RoutePath("/register-org", "POST")]
    public RegistrationResult RegisterOrganization(string name, string? handle)
    {
        return _userAccountService.RegisterOrganization(new OrganizationRegistrationData
        {
            Name = name,
            Handle = handle ?? string.Empty,
        });
    }

    [RoutePath("/assign-roles", "POST")]
    public object AssignRoles(string personHandle, string[] roleNames)
    {
        _userAccountService.AssignRoles(personHandle, roleNames);
        return new { personHandle, roleNames, success = true };
    }

    [RoutePath("/remove-roles", "POST")]
    public object RemoveRoles(string personHandle, string[] roleNames)
    {
        _userAccountService.RemoveRoles(personHandle, roleNames);
        return new { personHandle, roleNames, success = true };
    }

    [AnonymousAccess]
    [RoutePath("/{handle}", "GET")]
    public object? GetActor(string handle)
    {
        var profile = _userAccountService.GetProfile(handle);
        if (profile == null)
        {
            return null;
        }

        return new
        {
            profileHandle = profile.ProfileHandle,
            personHandle = profile.PersonHandle,
            name = profile.Name,
            deviceHandle = profile.DeviceHandle,
            roles = _userAccountService.GetRoles(profile.PersonHandle),
        };
    }
}
