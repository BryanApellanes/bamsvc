using Bam.Data.Objects;
using Bam.Data.Repositories;
using Bam.DependencyInjection;
using Bam.Protocol;
using Bam.Protocol.Server;
using Bam.Test;
using Bam.UserAccounts;

namespace Bam.Svc.Tests.Unit;

[UnitTestMenu("BamsvcServiceRegistration Should", Selector = "bsrs")]
public class BamsvcServiceRegistrationShould : UnitTestMenuContainer
{
    public BamsvcServiceRegistrationShould(ServiceRegistry serviceRegistry) : base(serviceRegistry)
    {
    }

    private static ServiceRegistry ComposeRegistry()
    {
        // the real composition Program.cs uses: BamServerOptions' initialized registry
        // (profile stack, key manager, object-data stack) plus bamsvc's account-confirmation wiring
        BamServerOptions options = new BamServerOptions();
        return options.ComponentRegistry.AddBamsvcAccountConfirmation("bamsvc-test");
    }

    [UnitTest]
    public void ResolveAccountConfirmationFromComposedRegistry()
    {
        After.Setup(reg =>
        {
            reg.For<ServiceRegistry>().Use(ComposeRegistry());
        })
        .When<ServiceRegistry>("resolves IAccountConfirmation from bamsvc's composed registry", (registry) =>
        {
            return new ResolutionOutcome(
                registry.Get<IAccountConfirmation>(),
                registry.Get<ICertificateManager>(),
                registry.Get<IRepository>(),
                registry.Get<IActor>());
        })
        .TheTest
        .ShouldPass<ResolutionOutcome>((because, outcome) =>
        {
            because.ItsTrue("IAccountConfirmation resolves to DeviceKeyAccountConfirmation", outcome.AccountConfirmation is DeviceKeyAccountConfirmation);
            because.ItsTrue("ICertificateManager resolves to CertificateManager", outcome.CertificateManager is CertificateManager);
            because.ItsTrue("IRepository resolves to ObjectDataRepository", outcome.Repository is ObjectDataRepository);
            because.ItsTrue("IActor issuer carries the server name as its handle", outcome.Issuer.Handle == "bamsvc-test");
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void ShareChallengeCustodyAcrossResolutions()
    {
        After.Setup(reg =>
        {
            reg.For<ServiceRegistry>().Use(ComposeRegistry());
        })
        .When<ServiceRegistry>("resolves IConfirmationChallengeRepository twice", (registry) =>
        {
            IConfirmationChallengeRepository first = registry.Get<IConfirmationChallengeRepository>();
            IConfirmationChallengeRepository second = registry.Get<IConfirmationChallengeRepository>();
            return new CustodyOutcome(ReferenceEquals(first, second));
        })
        .TheTest
        .ShouldPass<CustodyOutcome>((because, outcome) =>
        {
            because.ItsTrue("both resolutions share the same challenge repository - custody state is shared", outcome.SameInstance);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void ShareCustodyAcrossIncludedRegistries()
    {
        After.Setup(reg =>
        {
            reg.For<ServiceRegistry>().Use(ComposeRegistry());
        })
        .When<ServiceRegistry>("includes the composed registry into a second registry and resolves custody from both", (registry) =>
        {
            // mirrors the Program.cs topology: the HTTPS and TCP/UDP server registries
            // Include() the composed registry; custody must remain one instance across listeners
            ServiceRegistry secondRegistry = new ServiceRegistry();
            secondRegistry.Include(registry);
            return new IncludedCustodyOutcome(
                ReferenceEquals(registry.Get<IConfirmationChallengeRepository>(), secondRegistry.Get<IConfirmationChallengeRepository>()),
                ReferenceEquals(registry.Get<ICertificateManager>(), secondRegistry.Get<ICertificateManager>()));
        })
        .TheTest
        .ShouldPass<IncludedCustodyOutcome>((because, outcome) =>
        {
            because.ItsTrue("IConfirmationChallengeRepository is the same instance in both registries - custody is shared across listeners", outcome.SameChallengeRepository);
            because.ItsTrue("ICertificateManager is the same instance in both registries", outcome.SameCertificateManager);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    private sealed record ResolutionOutcome(
        IAccountConfirmation AccountConfirmation,
        ICertificateManager CertificateManager,
        IRepository Repository,
        IActor Issuer);

    private sealed record CustodyOutcome(bool SameInstance);

    private sealed record IncludedCustodyOutcome(bool SameChallengeRepository, bool SameCertificateManager);
}
