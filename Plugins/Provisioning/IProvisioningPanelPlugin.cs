using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// A hosting control panel as a plugin: the operations the host needs to create and run hosting accounts on a server. A
/// plugin opts in by implementing this interface on its provisioning class, which already implements
/// <see cref="IProvisioningPlugin"/> (usually through <see cref="Base.ProvisioningProviderBase"/>); the marker interface
/// and the base class do not change, so a provisioning plugin built on 3.0.0 still compiles and loads, and simply has
/// none of these operations. The host checks for this interface at run time and the manifest lists
/// <see cref="PluginCapabilities.ProvisioningPanel"/>.
/// </summary>
/// <remarks>
/// <para>
/// One plugin serves every server of its panel type. The server arrives in each call as a <see cref="PanelServerConnection"/>
/// with the credentials the host stores for it, so the plugin keeps no per-server state and holds no connection between calls.
/// </para>
/// <para>
/// Network. A panel plugin takes an <see cref="IPanelTransport"/> in its constructor and sends every request on the client
/// it creates. It never builds its own <see cref="HttpClient"/>: the host's address guard, private routes, timeouts and
/// no retry rule apply only to the transport. The manifest lists the module ids the plugin serves in
/// <see cref="Manifest.PluginManifest.ServerModules"/>.
/// </para>
/// <para>
/// Outcomes. Every operation answers a <see cref="PanelOperationOutcome"/> (the connection test answers a
/// <see cref="PanelConnectionOutcome"/>). A refusal, a missing account and an operation the panel does not have are
/// outcomes, never exceptions. A refusal carries a <see cref="PanelRefusalReason"/>, not panel text. A plugin throws only
/// when it does not know what happened, for example when the panel could not be reached or answered something that is not
/// its API; the host then treats the operation as unfinished and retries it, which is safe because of the next paragraph.
/// </para>
/// <para>
/// Idempotency. The host retries after a lost answer, so a repeat must end in the same state as the first call and must
/// answer a success. The create is keyed by <see cref="PanelCreateAccountContext.Username"/>, which the host chooses and
/// stores before the first call. After the first success a repeat answers: create <see cref="PanelOperationOutcome.AlreadyApplied"/>
/// with the same <see cref="PanelCreateResult.ProvisioningRef"/>; suspend and unsuspend <c>Succeeded</c> or <c>AlreadyApplied</c>
/// (an account already suspended, or already active, is success); terminate <c>Succeeded</c> or <c>AlreadyApplied</c>, and an
/// account that is already gone is <c>AlreadyApplied</c>, never <c>NotFound</c>; change package <c>Succeeded</c> or
/// <c>AlreadyApplied</c> when the account is already on the package; change password <c>Succeeded</c>. A plugin whose panel
/// refuses a repeat looks the account up after the refusal and answers from what it finds, instead of repeating the
/// refusal. <see cref="ProvisioningContract"/> checks these promises in a plugin's own tests.
/// </para>
/// <para>
/// Secrets. The server credentials, the password of a create or a change and the single sign on link are secrets. A
/// result never contains one, and neither does a log line or an exception message of the plugin: the records print them
/// masked, and a plugin must not undo that. The panel's own error text is never returned either, because it may carry a
/// user name, a domain or an address; the plugin logs it and answers a <see cref="PanelRefusalReason"/>.
/// </para>
/// <para>
/// Cancellation. Every operation takes a token and honours it; a cancelled call throws <see cref="OperationCanceledException"/>
/// and the host treats it like any unfinished call.
/// </para>
/// </remarks>
public interface IProvisioningPanelPlugin : IProvisioningPlugin
{
    /// <summary>
    /// Creates a hosting account. Idempotent on the user name: an account the panel already holds under
    /// <see cref="PanelCreateAccountContext.Username"/> is answered <see cref="PanelOperationOutcome.AlreadyApplied"/> with its
    /// reference, and is neither created again nor changed.
    /// </summary>
    /// <param name="context">The server, service, client, package, domain, user name and initial password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created, already existing or refused; the reference to keep is in the result.</returns>
    Task<PanelCreateResult> CreateAccountAsync(PanelCreateAccountContext context, CancellationToken ct);

    /// <summary>Suspends an account. Idempotent: an account that is already suspended is a success.</summary>
    /// <param name="context">The server, the account and the host's reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> or <c>Refused</c>.</returns>
    Task<PanelActionResult> SuspendAccountAsync(PanelSuspendContext context, CancellationToken ct);

    /// <summary>Re-activates a suspended account. Idempotent: an account that is already active is a success.</summary>
    /// <param name="context">The server and the account.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> or <c>Refused</c>.</returns>
    Task<PanelActionResult> UnsuspendAccountAsync(PanelAccountContext context, CancellationToken ct);

    /// <summary>
    /// Permanently removes an account and its data. Idempotent: an account that is already gone is
    /// <see cref="PanelOperationOutcome.AlreadyApplied"/>, because the wanted state, no account, is reached. The plugin
    /// answers <c>Succeeded</c> only when it removed the account now, and never <c>NotFound</c>. When the panel refuses and
    /// the account still exists, the answer is <c>Refused</c>, not <c>AlreadyApplied</c>.
    /// </summary>
    /// <param name="context">The server, the account and the host's reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c> or <c>Refused</c>.</returns>
    Task<PanelActionResult> TerminateAccountAsync(PanelTerminateContext context, CancellationToken ct);

    /// <summary>Moves an account to another package. Idempotent: an account already on the package is a success.</summary>
    /// <param name="context">The server, the account and the new package.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> or <c>Refused</c> (<see cref="PanelRefusalReason.PackageUnknown"/> for a package the panel does not have).</returns>
    Task<PanelActionResult> ChangePackageAsync(PanelChangePackageContext context, CancellationToken ct);

    /// <summary>
    /// Sets the password of an account. A panel never reveals a stored password, so a repeat with the same password simply
    /// sets it again and answers <c>Succeeded</c>.
    /// </summary>
    /// <param name="context">The server, the account and the new password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>NotFound</c> or <c>Refused</c> (<see cref="PanelRefusalReason.PasswordRejected"/> when the panel's policy rejects the password).</returns>
    Task<PanelActionResult> ChangePasswordAsync(PanelChangePasswordContext context, CancellationToken ct);

    /// <summary>
    /// Issues a time limited address that signs the client in to the account's control panel. Every call issues a new link;
    /// it is not idempotent by nature and does not change the account.
    /// </summary>
    /// <param name="context">The server, the account and the address of the browser that will open the link.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the link, <c>NotFound</c>, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<PanelSsoResult> CreateSsoLinkAsync(PanelSsoContext context, CancellationToken ct);

    /// <summary>Reads the resource figures the panel reports for an account. Read only; safe to repeat.</summary>
    /// <param name="context">The server and the account.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the figures, <c>NotFound</c>, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<PanelUsageResult> GetUsageAsync(PanelAccountContext context, CancellationToken ct);

    /// <summary>
    /// Checks that the host can reach the panel with the saved credentials. Unlike every other operation it does not throw
    /// for a panel that cannot be reached: that is the answer it exists to give. It changes nothing on the panel.
    /// </summary>
    /// <param name="context">The server to test.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The outcome with the panel's version and account count when it connected.</returns>
    Task<PanelConnectionTestResult> TestConnectionAsync(PanelTestConnectionContext context, CancellationToken ct);

    /// <summary>
    /// Lists the accounts the panel holds, one page per call, so the host can reconcile its services against the server.
    /// Read only. Following <see cref="PanelAccountListResult.NextCursor"/> to the end returns every account exactly once.
    /// </summary>
    /// <param name="context">The server and the cursor of the page wanted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A page, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<PanelAccountListResult> ListAccountsAsync(PanelListAccountsContext context, CancellationToken ct);
}
