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
        Command command = CommandFor(nameof(ActorRegistrationService.RegisterAgent));

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
        Command command = CommandFor(nameof(ActorRegistrationService.RegisterAgent));

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
        Command command = CommandFor(nameof(ActorRegistrationService.RegisterOrganization));

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
        Command command = CommandFor(nameof(ActorRegistrationService.AssignRoles));

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
        Command command = CommandFor(nameof(ActorRegistrationService.RemoveRoles));

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
        Command command = CommandFor(nameof(ActorRegistrationService.GetActor));

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
        Command command = CommandFor(nameof(ActorRegistrationService.GetActor));

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
        IAccessLevelProvider accessLevelProvider = Substitute.For<IAccessLevelProvider>();
        accessLevelProvider.GetAccessLevel(Arg.Any<IBamServerContext>()).Returns(BamAccess.Denied);

        AuthorizationCalculator calculator = new AuthorizationCalculator(accessLevelProvider);

        Command registerAgentCommand = CommandFor(nameof(ActorRegistrationService.RegisterAgent));
        Command getActorCommand = CommandFor(nameof(ActorRegistrationService.GetActor));

        IBamServerContext registerAgentContext = Substitute.For<IBamServerContext>();
        registerAgentContext.Command.Returns(registerAgentCommand);

        IBamServerContext getActorContext = Substitute.For<IBamServerContext>();
        getActorContext.Command.Returns(getActorCommand);

        When.A<AuthorizationCalculator>(
            "calculates authorization for anonymous callers on anonymous methods",
            calculator,
            (calc) =>
            {
                IAuthorizationCalculation registerResult = calc.CalculateAuthorization(registerAgentContext);
                IAuthorizationCalculation getActorResult = calc.CalculateAuthorization(getActorContext);
                return new AnonymousAccessOutcome(registerResult.Access, getActorResult.Access);
            })
        .TheTest
        .ShouldPass<AnonymousAccessOutcome>((because, _, outcome) =>
        {
            because.ItsTrue("RegisterAgent grants Execute access",
                outcome.RegisterAgentAccess == BamAccess.Execute);
            because.ItsTrue("GetActor grants Execute access",
                outcome.GetActorAccess == BamAccess.Execute);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void DenyAccessForAnonymousCallerOnAuthenticatedMethods()
    {
        IAccessLevelProvider accessLevelProvider = Substitute.For<IAccessLevelProvider>();
        accessLevelProvider.GetAccessLevel(Arg.Any<IBamServerContext>()).Returns(BamAccess.Denied);

        AuthorizationCalculator calculator = new AuthorizationCalculator(accessLevelProvider);

        Command registerOrgCommand = CommandFor(nameof(ActorRegistrationService.RegisterOrganization));
        Command assignRolesCommand = CommandFor(nameof(ActorRegistrationService.AssignRoles));
        Command removeRolesCommand = CommandFor(nameof(ActorRegistrationService.RemoveRoles));

        IBamServerContext registerOrgContext = Substitute.For<IBamServerContext>();
        registerOrgContext.Command.Returns(registerOrgCommand);

        IBamServerContext assignRolesContext = Substitute.For<IBamServerContext>();
        assignRolesContext.Command.Returns(assignRolesCommand);

        IBamServerContext removeRolesContext = Substitute.For<IBamServerContext>();
        removeRolesContext.Command.Returns(removeRolesCommand);

        When.A<AuthorizationCalculator>(
            "calculates authorization for anonymous callers on authenticated methods",
            calculator,
            (calc) =>
            {
                IAuthorizationCalculation registerOrgResult = calc.CalculateAuthorization(registerOrgContext);
                IAuthorizationCalculation assignRolesResult = calc.CalculateAuthorization(assignRolesContext);
                IAuthorizationCalculation removeRolesResult = calc.CalculateAuthorization(removeRolesContext);
                return new AuthenticatedAccessOutcome(registerOrgResult.Access, assignRolesResult.Access, removeRolesResult.Access);
            })
        .TheTest
        .ShouldPass<AuthenticatedAccessOutcome>((because, _, outcome) =>
        {
            because.ItsTrue("RegisterOrganization denies anonymous access",
                outcome.RegisterOrganizationAccess == BamAccess.Denied);
            because.ItsTrue("AssignRoles denies anonymous access",
                outcome.AssignRolesAccess == BamAccess.Denied);
            because.ItsTrue("RemoveRoles denies anonymous access",
                outcome.RemoveRolesAccess == BamAccess.Denied);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    private sealed record AnonymousAccessOutcome(BamAccess RegisterAgentAccess, BamAccess GetActorAccess);

    private sealed record AuthenticatedAccessOutcome(BamAccess RegisterOrganizationAccess, BamAccess AssignRolesAccess, BamAccess RemoveRolesAccess);
}
