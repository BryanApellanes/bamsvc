using Bam.Protocol;
using Bam.Protocol.Profile.Registration;
using Bam.Server;
using Bam.UserAccounts;

namespace Bam.Svc;

[RequiredAccess(BamAccess.Execute)]
[RoutePrefix("/api/registration")]
public class RegistrationService
{
    private readonly IUserAccountService _userAccountService;

    public RegistrationService(IUserAccountService userAccountService)
    {
        _userAccountService = userAccountService;
    }

    [AnonymousAccess(encryptionRequired: true)]
    [RoutePath("/register", "POST")]
    public RegistrationResult RegisterPerson(string firstName, string lastName, string? email, string? phone, string? handle)
    {
        return _userAccountService.RegisterPerson(new PersonRegistrationData
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email ?? string.Empty,
            Phone = phone ?? string.Empty,
            Handle = handle ?? string.Empty,
        });
    }

    [AnonymousAccess]
    [RoutePath("/profile/{handle}", "GET")]
    public object? GetProfile(string handle)
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
        };
    }
}
