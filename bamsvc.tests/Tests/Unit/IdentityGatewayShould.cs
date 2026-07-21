using Bam.DependencyInjection;
using Bam.Identity;
using Bam.Protocol;
using Bam.Protocol.Data.Server;
using Bam.Protocol.Server;
using Bam.Server;
using Bam.Svc;
using Bam.Test;
using NSubstitute;

namespace Bam.Svc.Tests.Unit;

[UnitTestMenu("IdentityGateway Should", Selector = "igw")]
public class IdentityGatewayShould : UnitTestMenuContainer
{
    public IdentityGatewayShould(ServiceRegistry serviceRegistry) : base(serviceRegistry)
    {
    }

    private static Command CommandFor(string methodName)
    {
        return new Command
        {
            TypeName = typeof(IdentityGateway).FullName!,
            MethodName = methodName,
        };
    }

    [UnitTest]
    public void HaveWebServiceAttribute()
    {
        When.A<Type>(
            "checks IdentityGateway has [WebService] attribute",
            typeof(IdentityGateway),
            (type) => Attribute.IsDefined(type, typeof(WebServiceAttribute)))
        .TheTest
        .ShouldPass(because =>
        {
            because.ItsTrue("[WebService] attribute is defined",
                Attribute.IsDefined(typeof(IdentityGateway), typeof(WebServiceAttribute)));
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void AllowAnonymousAccessForRegisterPerson()
    {
        Command command = CommandFor(nameof(IdentityGateway.RegisterPerson));

        When.A<Command>(
            "checks anonymous access for RegisterPerson",
            command,
            (cmd) => CommandAttributeResolver.IsAnonymousAccessAllowed(cmd))
        .TheTest
        .ShouldPass(because =>
        {
            because.ItsTrue("anonymous access is allowed",
                CommandAttributeResolver.IsAnonymousAccessAllowed(command));
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void AllowAnonymousAccessForGetProfile()
    {
        Command command = CommandFor(nameof(IdentityGateway.GetProfile));

        When.A<Command>(
            "checks anonymous access for GetProfile",
            command,
            (cmd) => CommandAttributeResolver.IsAnonymousAccessAllowed(cmd))
        .TheTest
        .ShouldPass(because =>
        {
            because.ItsTrue("anonymous access is allowed",
                CommandAttributeResolver.IsAnonymousAccessAllowed(command));
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void DelegateRegistrationToGatewayOutcomeLogic()
    {
        IRegistrationService registrationService = Substitute.For<IRegistrationService>();
        registrationService.RegisterPerson("Ada", "Lovelace", "ada@example.com", null, null)
            .Returns(new AccountData { PersonHandle = "ada-lovelace" });

        IdentityGateway gateway = new IdentityGateway(registrationService);

        When.A<IdentityGateway>(
            "registers a person through the pipeline-routed surface",
            gateway,
            (g) => g.RegisterPerson("Ada", "Lovelace", "ada@example.com", null, null))
        .TheTest
        .ShouldPass<GatewayOutcome>((because, _, outcome) =>
        {
            because.ItsTrue("status code is 200", outcome.StatusCode == 200);
            because.ItsTrue("body is a RegisterPersonResponse with the expected handle",
                outcome.Body is RegisterPersonResponse response && response.PersonHandle == "ada-lovelace");
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void ReturnNotFoundOutcomeForUnknownProfileHandle()
    {
        IRegistrationService registrationService = Substitute.For<IRegistrationService>();
        registrationService.GetProfile("unknown-handle").Returns((ProfileData?)null);

        IdentityGateway gateway = new IdentityGateway(registrationService);

        When.A<IdentityGateway>(
            "looks up an unknown profile through the pipeline-routed surface",
            gateway,
            (g) => g.GetProfile("unknown-handle"))
        .TheTest
        .ShouldPass<GatewayOutcome>((because, _, outcome) =>
        {
            because.ItsTrue("status code is 404", outcome.StatusCode == 404);
            because.ItsTrue("body is an ErrorResponse", outcome.Body is ErrorResponse);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }
}
