namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// The only way a panel plugin, a virtual server plugin or a registrar plugin reaches the other side over the network. The
/// host implements it and hands it to the plugin;
/// a plugin takes it in its constructor, next to the other services it asks for, and builds every request on the client
/// it returns.
/// </summary>
/// <remarks>
/// <para>
/// A plugin that takes this transport must never create its own <see cref="HttpClient"/>, handler or socket. The client from
/// <see cref="CreateClient()"/> is already routed through the host's rules, which a plugin could neither see nor keep
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
/// A host of 3.7.0 or later must not follow redirects for any client it gives to a plugin, this one included: a 3xx
/// answer reaches the plugin as it is, and the plugin decides whether the address it names is acceptable and sends a
/// new request itself. A client that followed a redirect would send the authorization header, and for some handlers
/// the body, to whatever address the answer names, which may be another host or an unintended path. Until the host a
/// plugin runs on meets this, the plugin sends its credentials only to the host it built the address for, and checks
/// the address of the response with <see cref="Http.PluginHttp.EnsureFinalHost"/>. When a 3xx answer reaches the
/// plugin it checks the scheme, host and port of the address the answer names before it follows it by hand.
/// <see cref="Http.PluginHttpContract"/> lets a plugin author prove the rule for a client in the plugin's own tests.
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

    /// <summary>
    /// Creates a client for one server, routed through the same rules as <see cref="CreateClient()"/>, and the one a plugin
    /// asks for whenever it talks to a server it has a <see cref="PanelServerConnection"/> for. Many hosting panels answer
    /// on a private port with a self signed certificate, and only the operator may decide to trust one. The host reads the
    /// operator's setting "accept a self signed certificate" for <see cref="PanelServerConnection.ServerId"/> and, when it
    /// is on, returns a client that accepts the certificate of that one host name and port and of nothing else: it never
    /// accepts a certificate for another host, never disables the check for every request, and keeps every other rule of
    /// the transport. The plugin only passes the server on; it has no switch of its own and never tries to build one.
    /// The default body returns <see cref="CreateClient()"/>, so a host that does not know the setting behaves as before and
    /// a plugin that calls this method works against any host.
    /// </summary>
    /// <param name="server">The server the requests of the client are for.</param>
    /// <returns>A client with no base address; use it for the one operation and let it go.</returns>
    HttpClient CreateClient(PanelServerConnection server)
    {
        return CreateClient();
    }
}
