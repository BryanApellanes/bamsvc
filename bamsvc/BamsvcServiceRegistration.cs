using Bam.Data.Objects;
using Bam.Data.Repositories;
using Bam.DependencyInjection;
using Bam.Protocol.Data.Common;
using Bam.Protocol.Profile;
using Bam.UserAccounts;

namespace Bam.Svc;

/// <summary>
/// Composes bamsvc's account-confirmation service wiring so the same registration used by
/// <c>Program.cs</c> is exercised by resolution tests.  Adopts
/// <see cref="UserAccountsServiceRegistration.AddDeviceKeyAccountConfirmation(ServiceRegistry)"/>
/// and supplies its host prerequisites (BryanApellanes/bamsvc#6).
/// </summary>
public static class BamsvcServiceRegistration
{
    /// <summary>
    /// Registers the device-key account-confirmation flow and its host prerequisites on the
    /// server's component registry:
    /// a durable <see cref="IRepository"/> (an <see cref="ObjectDataRepository"/> constructed
    /// eagerly as a singleton from the same encrypted object-data stack
    /// <c>AddEncryptedProfileRepository</c> registered — same root storage and key material, same
    /// store; that extension binds the repository transiently, so this is a distinct instance over
    /// the same store), an <see cref="Bam.Protocol.ICertificateManager"/>
    /// issuing on behalf of this server's actor identity (<see cref="ActorData"/> named
    /// <paramref name="serverName"/>), and the <see cref="Bam.UserAccounts.IAccountConfirmation"/>
    /// binding itself.
    /// <para>
    /// The registry must already carry the bindings <c>BamServerOptions.Initialize</c> establishes
    /// (<c>IProfileRepository</c>, <c>IKeyManager</c>, <c>ICompositeKeyCalculator</c>, and the
    /// object-data stack) — call this on a <c>BamServerOptions.ComponentRegistry</c>, not on an
    /// empty registry.
    /// </para>
    /// <para>
    /// Binding note: via <c>AddCertificateManager</c>, this call <b>replaces any process-wide
    /// <see cref="Bam.Protocol.IActor"/> binding</b> with the server's issuer identity. Request-time
    /// caller identity is unaffected — it resolves per request through <c>IActorResolver</c>, never
    /// from the registry's <see cref="Bam.Protocol.IActor"/> binding.
    /// </para>
    /// <para>
    /// Known limitation: the encrypted profile store is keyed per process start, so challenge
    /// custody effectively does not survive a restart; challenges expire in minutes, so the
    /// practical impact is bounded (pre-existing profile-stack behavior).
    /// </para>
    /// </summary>
    /// <param name="registry">The server component registry to add the bindings to.</param>
    /// <param name="serverName">This server's name, used as the certificate issuer's handle and display name.</param>
    /// <returns>The same registry, for fluent chaining.</returns>
    public static ServiceRegistry AddBamsvcAccountConfirmation(this ServiceRegistry registry, string serverName)
    {
        registry.For<IRepository>().UseSingleton<ObjectDataRepository>();
        registry.AddCertificateManager(new ActorData { Handle = serverName, Name = serverName });
        registry.AddDeviceKeyAccountConfirmation();

        return registry;
    }
}
