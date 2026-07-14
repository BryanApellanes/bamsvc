using Bam.DependencyInjection;
using Bam.Identity;
using Bam.Protocol.Data.Server;
using Bam.Svc;
using Bam.Test;
using NSubstitute;

namespace Bam.Svc.Tests.Unit;

[UnitTestMenu("IdentityGatewayRoutes should", Selector = "igr")]
public class IdentityGatewayRoutesShould : UnitTestMenuContainer
{
    public IdentityGatewayRoutesShould(ServiceRegistry serviceRegistry) : base(serviceRegistry)
    {
    }

    [UnitTest]
    public void DelegateSuccessfulRegistrationToRegistrationService()
    {
        IRegistrationService registrationService = Substitute.For<IRegistrationService>();
        registrationService.RegisterPerson("Ada", "Lovelace", "ada@example.com", null, null)
            .Returns(new AccountData { PersonHandle = "ada-lovelace" });

        IdentityGatewayRoutes routes = new IdentityGatewayRoutes(registrationService);
        PersonRegistrationRequest request = new PersonRegistrationRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
        };

        When.A<IdentityGatewayRoutes>(
            "builds the register outcome for a valid request",
            routes,
            (r) => r.BuildRegisterOutcome(request))
        .TheTest
        .ShouldPass(because =>
        {
            GatewayOutcome outcome = routes.BuildRegisterOutcome(request);
            because.ItsTrue("status code is 200", outcome.StatusCode == 200);
            because.ItsTrue("body is a RegisterPersonResponse with the expected handle",
                outcome.Body is RegisterPersonResponse response && response.PersonHandle == "ada-lovelace");
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void RejectRegistrationMissingRequiredFields()
    {
        IRegistrationService registrationService = Substitute.For<IRegistrationService>();
        IdentityGatewayRoutes routes = new IdentityGatewayRoutes(registrationService);
        PersonRegistrationRequest request = new PersonRegistrationRequest
        {
            FirstName = "",
            LastName = "Lovelace",
        };

        When.A<IdentityGatewayRoutes>(
            "builds the register outcome for a request missing FirstName",
            routes,
            (r) => r.BuildRegisterOutcome(request))
        .TheTest
        .ShouldPass(because =>
        {
            GatewayOutcome outcome = routes.BuildRegisterOutcome(request);
            because.ItsTrue("status code is 400", outcome.StatusCode == 400);
            because.ItsTrue("body is an ErrorResponse", outcome.Body is ErrorResponse);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void DelegateProfileLookupToRegistrationService()
    {
        IRegistrationService registrationService = Substitute.For<IRegistrationService>();
        registrationService.GetProfile("ada-lovelace").Returns(new ProfileData
        {
            ProfileHandle = "profile-1",
            PersonHandle = "ada-lovelace",
            Name = "Ada Lovelace",
            DeviceHandle = "device-1",
        });

        IdentityGatewayRoutes routes = new IdentityGatewayRoutes(registrationService);

        When.A<IdentityGatewayRoutes>(
            "builds the profile outcome for a known handle",
            routes,
            (r) => r.BuildProfileOutcome("ada-lovelace"))
        .TheTest
        .ShouldPass(because =>
        {
            GatewayOutcome outcome = routes.BuildProfileOutcome("ada-lovelace");
            because.ItsTrue("status code is 200", outcome.StatusCode == 200);
            because.ItsTrue("body is the expected ProfileData",
                outcome.Body is ProfileData profile && profile.PersonHandle == "ada-lovelace");
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void ReturnNotFoundForUnknownProfileHandle()
    {
        IRegistrationService registrationService = Substitute.For<IRegistrationService>();
        registrationService.GetProfile("unknown-handle").Returns((ProfileData?)null);

        IdentityGatewayRoutes routes = new IdentityGatewayRoutes(registrationService);

        When.A<IdentityGatewayRoutes>(
            "builds the profile outcome for an unknown handle",
            routes,
            (r) => r.BuildProfileOutcome("unknown-handle"))
        .TheTest
        .ShouldPass(because =>
        {
            GatewayOutcome outcome = routes.BuildProfileOutcome("unknown-handle");
            because.ItsTrue("status code is 404", outcome.StatusCode == 404);
            because.ItsTrue("body is an ErrorResponse", outcome.Body is ErrorResponse);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }
}
