using Bam.DependencyInjection;
using Bam.Protocol;
using Bam.Protocol.Server;
using Bam.Server;
using Bam.Svc;
using Bam.Test;
using NSubstitute;

namespace Bam.Svc.Tests.Unit;

[UnitTestMenu("ActorRegistrationService Should", Selector = "ars")]
public class ActorRegistrationServiceShould : UnitTestMenuContainer
{
    public ActorRegistrationServiceShould(ServiceRegistry serviceRegistry) : base(serviceRegistry)
    {
    }

    private static Command CommandFor(string methodName)
    {
        return new Command
        {
            TypeName = typeof(ActorRegistrationService).FullName!,
            MethodName = methodName,
        };
    }

    [UnitTest]
    public void HaveWebServiceAttribute()
    {
        When.A<Type>(
            "checks ActorRegistrationService has [WebService] attribute",
            typeof(ActorRegistrationService),
            (type) => Attribute.IsDefined(type, typeof(WebServiceAttribute)))
        .TheTest
        .ShouldPass(because =>
        {
            because.ItsTrue("[WebService] attribute is defined",
                Attribute.IsDefined(typeof(ActorRegistrationService), typeof(WebServiceAttribute)));
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void AllowAnonymousAccessForRegisterAgent()
    {
        var command = CommandFor(nameof(ActorRegistrationService.RegisterAgent));

        When.A<Command>(
            "checks anonymous access for RegisterAgent",
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
    public void RequireEncryptionForRegisterAgent()
    {
        var command = CommandFor(nameof(ActorRegistrationService.RegisterAgent));

        When.A<Command>(
            "checks encryption requirement for RegisterAgent",
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
    public void NotAllowAnonymousAccessForRegisterOrganization()
    {
        var command = CommandFor(nameof(ActorRegistrationService.RegisterOrganization));

        When.A<Command>(
            "checks anonymous access is denied for RegisterOrganization",
            command,
            (cmd) => CommandAttributeResolver.IsAnonymousAccessAllowed(cmd))
        .TheTest
        .ShouldPass(because =>
        {
            because.ItsTrue("anonymous access is not allowed",
                !CommandAttributeResolver.IsAnonymousAccessAllowed(command));
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void NotAllowAnonymousAccessForAssignRoles()
    {
        var command = CommandFor(nameof(ActorRegistrationService.AssignRoles));

        When.A<Command>(
            "checks anonymous access is denied for AssignRoles",
            command,
            (cmd) => CommandAttributeResolver.IsAnonymousAccessAllowed(cmd))
        .TheTest
        .ShouldPass(because =>
        {
            because.ItsTrue("anonymous access is not allowed",
                !CommandAttributeResolver.IsAnonymousAccessAllowed(command));
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void NotAllowAnonymousAccessForRemoveRoles()
    {
        var command = CommandFor(nameof(ActorRegistrationService.RemoveRoles));

        When.A<Command>(
            "checks anonymous access is denied for RemoveRoles",
            command,
            (cmd) => CommandAttributeResolver.IsAnonymousAccessAllowed(cmd))
        .TheTest
        .ShouldPass(because =>
        {
            because.ItsTrue("anonymous access is not allowed",
                !CommandAttributeResolver.IsAnonymousAccessAllowed(command));
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void AllowAnonymousAccessForGetActor()
    {
        var command = CommandFor(nameof(ActorRegistrationService.GetActor));

        When.A<Command>(
            "checks anonymous access for GetActor",
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
    public void NotRequireEncryptionForGetActor()
    {
        var command = CommandFor(nameof(ActorRegistrationService.GetActor));

        When.A<Command>(
            "checks encryption is not required for GetActor",
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
    public void GrantAccessForAnonymousCallerOnAnonymousMethods()
    {
        var accessLevelProvider = Substitute.For<IAccessLevelProvider>();
        accessLevelProvider.GetAccessLevel(Arg.Any<IBamServerContext>()).Returns(BamAccess.Denied);

        var calculator = new AuthorizationCalculator(accessLevelProvider);

        var registerAgentCommand = CommandFor(nameof(ActorRegistrationService.RegisterAgent));
        var getActorCommand = CommandFor(nameof(ActorRegistrationService.GetActor));

        var registerAgentContext = Substitute.For<IBamServerContext>();
        registerAgentContext.Command.Returns(registerAgentCommand);

        var getActorContext = Substitute.For<IBamServerContext>();
        getActorContext.Command.Returns(getActorCommand);

        When.A<AuthorizationCalculator>(
            "calculates authorization for anonymous callers on anonymous methods",
            calculator,
            (calc) =>
            {
                var registerResult = calc.CalculateAuthorization(registerAgentContext);
                var getActorResult = calc.CalculateAuthorization(getActorContext);
                return new { registerAccess = registerResult.Access, getActorAccess = getActorResult.Access };
            })
        .TheTest
        .ShouldPass(because =>
        {
            var registerResult = calculator.CalculateAuthorization(registerAgentContext);
            var getActorResult = calculator.CalculateAuthorization(getActorContext);

            because.ItsTrue("RegisterAgent grants Execute access",
                registerResult.Access == BamAccess.Execute);
            because.ItsTrue("GetActor grants Execute access",
                getActorResult.Access == BamAccess.Execute);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void DenyAccessForAnonymousCallerOnAuthenticatedMethods()
    {
        var accessLevelProvider = Substitute.For<IAccessLevelProvider>();
        accessLevelProvider.GetAccessLevel(Arg.Any<IBamServerContext>()).Returns(BamAccess.Denied);

        var calculator = new AuthorizationCalculator(accessLevelProvider);

        var registerOrgCommand = CommandFor(nameof(ActorRegistrationService.RegisterOrganization));
        var assignRolesCommand = CommandFor(nameof(ActorRegistrationService.AssignRoles));
        var removeRolesCommand = CommandFor(nameof(ActorRegistrationService.RemoveRoles));

        var registerOrgContext = Substitute.For<IBamServerContext>();
        registerOrgContext.Command.Returns(registerOrgCommand);

        var assignRolesContext = Substitute.For<IBamServerContext>();
        assignRolesContext.Command.Returns(assignRolesCommand);

        var removeRolesContext = Substitute.For<IBamServerContext>();
        removeRolesContext.Command.Returns(removeRolesCommand);

        When.A<AuthorizationCalculator>(
            "calculates authorization for anonymous callers on authenticated methods",
            calculator,
            (calc) =>
            {
                var registerOrgResult = calc.CalculateAuthorization(registerOrgContext);
                var assignRolesResult = calc.CalculateAuthorization(assignRolesContext);
                var removeRolesResult = calc.CalculateAuthorization(removeRolesContext);
                return new
                {
                    registerOrgAccess = registerOrgResult.Access,
                    assignRolesAccess = assignRolesResult.Access,
                    removeRolesAccess = removeRolesResult.Access,
                };
            })
        .TheTest
        .ShouldPass(because =>
        {
            var registerOrgResult = calculator.CalculateAuthorization(registerOrgContext);
            var assignRolesResult = calculator.CalculateAuthorization(assignRolesContext);
            var removeRolesResult = calculator.CalculateAuthorization(removeRolesContext);

            because.ItsTrue("RegisterOrganization denies anonymous access",
                registerOrgResult.Access == BamAccess.Denied);
            because.ItsTrue("AssignRoles denies anonymous access",
                assignRolesResult.Access == BamAccess.Denied);
            because.ItsTrue("RemoveRoles denies anonymous access",
                removeRolesResult.Access == BamAccess.Denied);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }
}
