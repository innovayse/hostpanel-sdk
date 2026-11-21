using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Identity;

namespace Hostpanel.Sdk.Plugins.Fulfilment;

/// <summary>
/// Handed to <see cref="IFulfilmentPlugin.OnServicePlanChangedAsync"/> when a client's service moved to another plan or
/// its quantity changed. Since 3.12.0; a host built on an earlier version never sends it.
/// </summary>
/// <param name="ServiceId">The host's id of the service that changed.</param>
/// <param name="ClientId">The host's client id.</param>
/// <param name="ClientRef">
/// The plugin's own reference to the client: the same opaque value an endpoint call of this plugin for this client
/// carries in <see cref="Endpoints.PluginCaller.ClientRef"/>, so what a plugin sold can be joined with what its endpoints
/// see. It differs between plugins and cannot be turned back into <paramref name="ClientId"/>.
/// </param>
/// <param name="Account">The client's external account, or null when the client is not linked to one.</param>
/// <param name="Previous">The service's line before the change.</param>
/// <param name="Current">The service's line after the change.</param>
/// <param name="Kind">Whether the change is an upgrade, a downgrade or a change of quantity.</param>
/// <param name="ChangedAt">When the host applied the change (UTC).</param>
/// <param name="IdempotencyKey">
/// Identifies this one change. Delivery is at-least-once, so the same change can arrive again with the same key; a plugin
/// that has applied the key answers normally without applying it twice. A later change of the same service has a new key.
/// </param>
public sealed partial record ServicePlanChangedContext(
    int ServiceId,
    int ClientId,
    [property: SecretMember] string ClientRef,
    ExternalAccount? Account,
    FulfilmentLine Previous,
    FulfilmentLine Current,
    ServicePlanChangeKind Kind,
    DateTimeOffset ChangedAt,
    string IdempotencyKey);
