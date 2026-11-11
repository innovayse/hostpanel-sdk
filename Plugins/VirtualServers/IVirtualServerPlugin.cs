using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// A virtual server or cloud platform as a plugin: the operations the host needs to create and run virtual servers for its
/// clients. A virtual server is a service provisioned on a server the host knows, exactly like a hosting account, so the
/// plugin type stays <see cref="PluginType.Provisioning"/>: a plugin opts in by implementing this interface on its
/// provisioning class, which already implements <see cref="IProvisioningPlugin"/> (usually through
/// <see cref="Base.ProvisioningProviderBase"/>). The marker interface, the base class and
/// <see cref="IProvisioningPanelPlugin"/> do not change, so an existing provisioning plugin still compiles and loads, and
/// simply has none of these operations. The host checks for this interface at run time and the manifest lists
/// <see cref="PluginCapabilities.VirtualServer"/>.
/// </summary>
/// <remarks>
/// <para>
/// The vocabulary is the one of <see cref="IProvisioningPanelPlugin"/>: the platform arrives in each call as a
/// <see cref="PanelServerConnection"/>, the service as a <see cref="PanelService"/> whose
/// <see cref="PanelService.ProvisioningRef"/> is the reference <see cref="CreateServerAsync"/> returned, and the answers are
/// <see cref="PanelActionResult"/> and <see cref="ValueResult{TValue,TReason}"/> with a <see cref="PanelOperationOutcome"/> and a
/// <see cref="PanelRefusalReason"/>. One plugin serves every platform of its type, keeps no per-server state and holds no
/// connection between calls. The manifest lists the module ids the plugin serves in
/// <see cref="Manifest.PluginManifest.ServerModules"/>.
/// </para>
/// <para>
/// Network. The plugin takes an <see cref="IPanelTransport"/> in its constructor and sends every request on the client it
/// creates; it never builds its own <see cref="HttpClient"/>. A platform that is not reached over HTTP is not served by this
/// contract.
/// </para>
/// <para>
/// Outcomes. A refusal, a missing server and an operation the platform does not have are outcomes, never exceptions. A
/// refusal carries a <see cref="PanelRefusalReason"/>, not the platform's text. A plugin throws only when it does not know
/// what happened, for example when the platform could not be reached; the host then treats the operation as unfinished and
/// retries it, which is safe because of the next paragraph.
/// </para>
/// <para>
/// Idempotency. The host retries after a lost answer, so a repeat must end in the same state as the first call and answer a
/// success. The create is keyed by <see cref="VirtualServerCreateContext.Name"/>, which the host chooses and stores before
/// the first call: a server the platform already holds under that name is the earlier attempt, answered
/// <see cref="PanelOperationOutcome.AlreadyApplied"/> with the same <see cref="VirtualServerInstance.ProvisioningRef"/>.
/// Start, stop, suspend, unsuspend and change plan answer <c>Succeeded</c> or <c>AlreadyApplied</c> (a server already
/// running, stopped, suspended, active or on the plan is success). Terminate answers <c>Succeeded</c> or
/// <c>AlreadyApplied</c>, and a server that is already gone is <c>AlreadyApplied</c>, never <c>NotFound</c>. Reboot, rebuild,
/// root password reset and console sessions are not idempotent by nature: each call does the work again and answers
/// <c>Succeeded</c>, so the host sends them only on a request and retries only after an unfinished call.
/// <see cref="VirtualServerContract"/> checks these promises in a plugin's own tests.
/// </para>
/// <para>
/// Secrets. The platform credentials, the root password, a console address and the root password a platform generated are
/// secrets. A result never contains one outside the records documented to hold it, which print it masked, and neither does
/// a log line or an exception message of the plugin. The platform's own error text is never returned either: the plugin
/// logs it and answers a <see cref="PanelRefusalReason"/>.
/// </para>
/// <para>
/// Cancellation. Every operation takes a token and honours it; a cancelled call throws <see cref="OperationCanceledException"/>
/// and the host treats it like any unfinished call.
/// </para>
/// <para>
/// Optional operations. The listing of the servers a platform holds is the separate interface
/// <see cref="IVirtualServerListPlugin"/> with its own capability, and a host treats a plugin that does not implement it as
/// <see cref="PanelOperationOutcome.NotSupported"/>. New operations always arrive as new interfaces, never as members of an
/// existing one, so that a plugin built on an earlier version keeps compiling and loading.
/// </para>
/// </remarks>
public interface IVirtualServerPlugin : IProvisioningPlugin
{
    /// <summary>
    /// Creates a virtual server from a plan and an image. Idempotent on the name: a server the platform already holds under
    /// <see cref="VirtualServerCreateContext.Name"/> is answered <see cref="PanelOperationOutcome.AlreadyApplied"/> with its
    /// reference, and is neither created again nor changed.
    /// </summary>
    /// <param name="context">The platform, the service, the client, the plan, the image, the name and the root password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created or existing server, or a refusal (<see cref="PanelRefusalReason.PackageUnknown"/> for a plan, <see cref="PanelRefusalReason.ImageUnknown"/>, <see cref="PanelRefusalReason.NameInvalid"/> and <see cref="PanelRefusalReason.LimitReached"/> among others).</returns>
    Task<ValueResult<VirtualServerInstance, PanelRefusalReason>> CreateServerAsync(VirtualServerCreateContext context, CancellationToken ct);

    /// <summary>
    /// Powers a server on. Idempotent: a server that is already running is a success. A server the host has suspended is not
    /// started: the answer is <c>Refused</c> with <see cref="PanelRefusalReason.AccessDenied"/>, and the server stays off until
    /// <see cref="UnsuspendServerAsync"/>.
    /// </summary>
    /// <param name="context">The platform and the server.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> or <c>Refused</c> (<see cref="PanelRefusalReason.AccessDenied"/> for a suspended server).</returns>
    Task<PanelActionResult> StartServerAsync(PanelAccountContext context, CancellationToken ct);

    /// <summary>Powers a server off. Idempotent: a server that is already stopped is a success.</summary>
    /// <param name="context">The platform and the server.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> or <c>Refused</c>.</returns>
    Task<PanelActionResult> StopServerAsync(PanelAccountContext context, CancellationToken ct);

    /// <summary>Restarts a server. Not idempotent: every call restarts it again.</summary>
    /// <param name="context">The platform and the server.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>NotFound</c> or <c>Refused</c>.</returns>
    Task<PanelActionResult> RebootServerAsync(PanelAccountContext context, CancellationToken ct);

    /// <summary>
    /// Suspends a server: it is powered off and the client can neither start it nor change it until
    /// <see cref="UnsuspendServerAsync"/>. A platform with no suspension of its own powers the server off and the plugin keeps
    /// the server from being started by the client in whatever way the platform allows. Idempotent: a server that is already
    /// suspended is a success.
    /// </summary>
    /// <param name="context">The platform, the server and the host's reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> or <c>Refused</c>.</returns>
    Task<PanelActionResult> SuspendServerAsync(PanelSuspendContext context, CancellationToken ct);

    /// <summary>Lifts a suspension. Idempotent: a server that is not suspended is a success. It does not power the server on; the host calls <see cref="StartServerAsync"/> for that.</summary>
    /// <param name="context">The platform and the server.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> or <c>Refused</c>.</returns>
    Task<PanelActionResult> UnsuspendServerAsync(PanelAccountContext context, CancellationToken ct);

    /// <summary>
    /// Permanently deletes a server and its disk. Idempotent: a server that is already gone is
    /// <see cref="PanelOperationOutcome.AlreadyApplied"/>, because the wanted state, no server, is reached. The plugin answers
    /// <c>Succeeded</c> only when it deleted the server now, and never <c>NotFound</c>. When the platform refuses and the
    /// server still exists, the answer is <c>Refused</c>, not <c>AlreadyApplied</c>.
    /// </summary>
    /// <param name="context">The platform, the server and the host's reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c> or <c>Refused</c>.</returns>
    Task<PanelActionResult> TerminateServerAsync(PanelTerminateContext context, CancellationToken ct);

    /// <summary>
    /// Reinstalls a server from an image, erasing its disk. Not idempotent: every call erases the disk again, so the host
    /// retries it only after an unfinished call.
    /// </summary>
    /// <param name="context">The platform, the server, the image and the root password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the root password the platform generated when it could not set the requested one, <c>NotFound</c> or a refusal (<see cref="PanelRefusalReason.ImageUnknown"/> among others).</returns>
    Task<ValueResult<VirtualServerRootPassword, PanelRefusalReason>> RebuildServerAsync(VirtualServerRebuildContext context, CancellationToken ct);

    /// <summary>Moves a server to another plan. Idempotent: a server already on the plan is a success.</summary>
    /// <param name="context">The platform, the server and the new plan (<see cref="PanelChangePackageContext.Package"/>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> or <c>Refused</c> (<see cref="PanelRefusalReason.PackageUnknown"/> for a plan the platform does not have).</returns>
    Task<PanelActionResult> ChangePlanAsync(PanelChangePackageContext context, CancellationToken ct);

    /// <summary>
    /// Sets the root password of a server. A platform never reveals a stored password, so a repeat with the same password
    /// simply sets it again and answers <c>Succeeded</c>.
    /// </summary>
    /// <param name="context">The platform, the server and the new password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the root password the platform generated when it could not set the requested one, <c>NotFound</c> or <c>Refused</c> (<see cref="PanelRefusalReason.PasswordRejected"/> when the platform's policy rejects the password).</returns>
    Task<ValueResult<VirtualServerRootPassword, PanelRefusalReason>> ResetRootPasswordAsync(PanelChangePasswordContext context, CancellationToken ct);

    /// <summary>
    /// Opens a console session (a VNC or web console) on a server. Every call issues a new address; it is not idempotent by
    /// nature and does not change the server.
    /// </summary>
    /// <param name="context">The platform and the server.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the address, <c>NotFound</c>, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<VirtualServerConsole, PanelRefusalReason>> CreateConsoleSessionAsync(PanelAccountContext context, CancellationToken ct);

    /// <summary>Reads the power state and resource figures the platform reports for a server. Read only; safe to repeat.</summary>
    /// <param name="context">The platform and the server.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the figures, <c>NotFound</c>, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<VirtualServerUsage, PanelRefusalReason>> GetServerUsageAsync(PanelAccountContext context, CancellationToken ct);

    /// <summary>Lists the images the platform can create or rebuild a server from. Read only; safe to repeat.</summary>
    /// <param name="context">The platform.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with every image once (an empty list when there is none), <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<IReadOnlyList<VirtualServerImage>, PanelRefusalReason>> ListImagesAsync(VirtualServerCatalogContext context, CancellationToken ct);

    /// <summary>Lists the plans the platform can size a server by. Read only; safe to repeat.</summary>
    /// <param name="context">The platform.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with every plan once (an empty list when there is none), <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<IReadOnlyList<VirtualServerPlan>, PanelRefusalReason>> ListPlansAsync(VirtualServerCatalogContext context, CancellationToken ct);

    /// <summary>
    /// Checks that the host can reach the platform with the saved credentials. Unlike every other operation it does not throw
    /// for a platform that cannot be reached: that is the answer it exists to give. It changes nothing on the platform.
    /// </summary>
    /// <param name="context">The platform to test.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The outcome with the platform's version and server count when it connected.</returns>
    Task<PanelConnectionTestResult> TestServerConnectionAsync(PanelTestConnectionContext context, CancellationToken ct);
}
