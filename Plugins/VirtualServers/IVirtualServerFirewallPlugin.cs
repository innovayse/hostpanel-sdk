using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// The firewall rules of a virtual server, as a separate opt in: a virtual server plugin implements it next to
/// <see cref="IVirtualServerPlugin"/> and lists <see cref="PluginCapabilities.VirtualServerFirewall"/> in its manifest. A host
/// treats a plugin that does not implement it as <see cref="PanelOperationOutcome.NotSupported"/>. It is a separate interface
/// because a new operation always arrives as a new interface, never as a member of an existing one.
/// </summary>
/// <remarks>
/// The rules of <see cref="IVirtualServerPlugin"/> hold here: the platform arrives with its stored credentials, a refusal and
/// a missing server are outcomes and never exceptions, a repeat ends in the same state as the first call, and the platform's
/// own text is never returned.
/// </remarks>
public interface IVirtualServerFirewallPlugin : IVirtualServerPlugin
{
    /// <summary>Reads the rules of a server, in the order they apply. Read only; safe to repeat. Every rule is valid as <see cref="VirtualServerFirewallRule"/> describes.</summary>
    /// <param name="context">The platform and the server.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the rules (an empty list is a server without rules), <c>NotFound</c>, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<IReadOnlyList<VirtualServerFirewallRule>, PanelRefusalReason>> GetFirewallRulesAsync(PanelAccountContext context, CancellationToken ct);

    /// <summary>
    /// Replaces the whole set of rules of a server with the one the host sends. Idempotent: a server that already has exactly
    /// this set answers <see cref="PanelOperationOutcome.Succeeded"/> or <see cref="PanelOperationOutcome.AlreadyApplied"/>.
    /// </summary>
    /// <param name="context">The platform, the server and the rules wanted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>Pending</c> while the platform applies it, <c>NotFound</c> or <c>Refused</c> (<see cref="PanelRefusalReason.LimitReached"/> when the platform allows no more rules).</returns>
    Task<PanelActionResult> SetFirewallRulesAsync(VirtualServerFirewallContext context, CancellationToken ct);
}
