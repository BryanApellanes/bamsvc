using Bam.DependencyInjection;
using Bam.Protocol;
using Bam.Protocol.Server;
using Bam.Svc;
using Bam.Test;
using NSubstitute;

namespace Bam.Svc.Tests.Unit;

[UnitTestMenu("RegistrationService Should", Selector = "rss")]
public class RegistrationServiceShould : UnitTestMenuContainer
{
    public RegistrationServiceShould(ServiceRegistry serviceRegistry) : base(serviceRegistry)
    {
    }

    private static Command CommandFor(string methodName)
    {
        return new Command
        {
            TypeName = typeof(RegistrationService).FullName!,
            MethodName = methodName,
        };
    }

    [UnitTest]
    public void AllowAnonymousAccessForRegisterPerson()
    {
        var command = CommandFor(nameof(RegistrationService.RegisterPerson));

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
    public void RequireEncryptionForRegisterPerson()
    {
        var command = CommandFor(nameof(RegistrationService.RegisterPerson));

        When.A<Command>(
            "checks encryption requirement for RegisterPerson",
            command,
            (cmd) => CommandAttributeResolver.IsEncryptionRequired(cmd))
        .TheTest
        .ShouldPass(because =>
        {
            because.ItsTrue("encryption is required",
                CommandAttributeResolver.IsEncryptionRequired(command));
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void AllowAnonymousAccessForGetProfile()
    {
        var command = CommandFor(nameof(RegistrationService.GetProfile));

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
    public void NotRequireEncryptionForGetProfile()
    {
        var command = CommandFor(nameof(RegistrationService.GetProfile));

        When.A<Command>(
            "checks encryption is not required for GetProfile",
            command,
            (cmd) => CommandAttributeResolver.IsEncryptionRequired(cmd))
        .TheTest
        .ShouldPass(because =>
        {
            because.ItsTrue("encryption is not required",
                !CommandAttributeResolver.IsEncryptionRequired(command));
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void GrantAccessForAnonymousCallerOnBothMethods()
    {
        var accessLevelProvider = Substitute.For<IAccessLevelProvider>();
        accessLevelProvider.GetAccessLevel(Arg.Any<IBamServerContext>()).Returns(BamAccess.Denied);

        var calculator = new AuthorizationCalculator(accessLevelProvider);

        var registerCommand = CommandFor(nameof(RegistrationService.RegisterPerson));
        var profileCommand = CommandFor(nameof(RegistrationService.GetProfile));

        var registerContext = Substitute.For<IBamServerContext>();
        registerContext.Command.Returns(registerCommand);

        var profileContext = Substitute.For<IBamServerContext>();
        profileContext.Command.Returns(profileCommand);

        When.A<AuthorizationCalculator>(
            "calculates authorization for anonymous callers",
            calculator,
            (calc) =>
            {
                var registerResult = calc.CalculateAuthorization(registerContext);
                var profileResult = calc.CalculateAuthorization(profileContext);
                return new { registerAccess = registerResult.Access, profileAccess = profileResult.Access };
            })
        .TheTest
        .ShouldPass(because =>
        {
            var registerResult = calculator.CalculateAuthorization(registerContext);
            var profileResult = calculator.CalculateAuthorization(profileContext);

            because.ItsTrue("RegisterPerson grants Execute access",
                registerResult.Access == BamAccess.Execute);
            because.ItsTrue("GetProfile grants Execute access",
                profileResult.Access == BamAccess.Execute);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }
}
