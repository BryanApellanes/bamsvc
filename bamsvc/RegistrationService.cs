using Bam.Protocol;
using Bam.Protocol.Data;
using Bam.Protocol.Data.Server;
using Bam.Protocol.Profile;
using Bam.Protocol.Profile.Registration;

namespace Bam.Svc;

[RequiredAccess(BamAccess.Execute)]
public class RegistrationService
{
    private readonly IAccountManager _accountManager;
    private readonly IProfileManager _profileManager;

    public RegistrationService(IAccountManager accountManager, IProfileManager profileManager)
    {
        _accountManager = accountManager;
        _profileManager = profileManager;
    }

    [AnonymousAccess(encryptionRequired: true)]
    public AccountData RegisterPerson(string firstName, string lastName, string? email, string? phone, string? handle)
    {
        var registrationData = new PersonRegistrationData
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email ?? string.Empty,
            Phone = phone ?? string.Empty,
            Handle = handle ?? string.Empty,
        };

        return _accountManager.RegisterAccount(registrationData);
    }

    [AnonymousAccess]
    public object? GetProfile(string handle)
    {
        var profile = _profileManager.FindProfileByHandle(handle);
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
