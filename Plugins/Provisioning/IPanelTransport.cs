namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// The only way a panel plugin reaches its panel over the network. The host implements it and hands it to the plugin;
/// a plugin takes it in its constructor, next to the other services it asks for, and builds every request on the client
/// it returns.
/// </summary>
/// <remarks>
/// <para>
/// A panel plugin must never create its own <see cref="HttpClient"/>, handler or socket. The client from
/// <see cref="CreateClient"/> is already routed through the host's rules, which a plugin could neither see nor keep
/// if it went around them: the host's outbound address guard (a server address that points into the host's own network
/// is refused before a request leaves), its private route map for servers reached over a private path, its timeouts,
/// and its no retry rule. The last one matters to a plugin: a panel call can carry a password or change an account,
/// so the transport never repeats a request on its own, and the plugin owns every retry decision through the
/// idempotency its operations promise.
/// </para>
/// <para>
/// The client has no base address and no default headers. The plugin builds absolute addresses from
/// <see cref="PanelServerConnection"/> (honouring <see cref="PanelServerConnection.UseSsl"/> and a null
/// <see cref="PanelServerConnection.Port"/> as its own default), and sets its own authorization header per request, so
/// a credential of one server is never attached to a request for another. A refused address, a timeout or a
/// connection failure surfaces as an exception from the client; <see cref="IProvisioningPanelPlugin.TestConnectionAsync"/>
/// turns it into <see cref="Enums.PanelConnectionOutcome.Unreachable"/>, and every other operation lets it propagate
/// because the outcome is then unknown.
/// </para>
/// <para>
/// A plugin does not log request addresses that carry arguments in the query string, and does not dispose the
/// handler: the host owns the connection pool behind the client.
/// </para>
/// </remarks>
public interface IPanelTransport
{
    /// <summary>Creates a client routed through the host's address guard, private route map, timeouts and no retry rule.</summary>
    /// <returns>A client with no base address; use it for the one operation and let it go.</returns>
    HttpClient CreateClient();
}
