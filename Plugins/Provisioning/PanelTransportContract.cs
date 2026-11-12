using System.Security.Authentication;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Checks a plugin author runs in the plugin's own tests, with a <see cref="SelfSignedListener"/>, to prove that a
/// transport keeps the promise of <see cref="IPanelTransport.CreateClient(PanelServerConnection)"/>: the choice to trust
/// a self signed certificate belongs to the operator and applies to one host name and port of one server only. Each
/// check throws <see cref="PluginContractViolationException"/> naming the promise that was broken. Nothing here depends
/// on a test framework. The host's own address guard may refuse a loopback address, so these checks are for the
/// transport double of a plugin's tests and not for a transport behind that guard.
/// </summary>
public static class PanelTransportContract
{
    /// <summary>
    /// Asserts that a client that was not created for a server with the operator's setting refuses the self signed
    /// certificate: both <see cref="IPanelTransport.CreateClient()"/> and the per server client of a server whose
    /// setting is off must fail the TLS handshake, so trust is never global and never on by default.
    /// </summary>
    /// <param name="transport">The transport under test.</param>
    /// <param name="serverWithSettingOff">
    /// A server the operator did not allow a self signed certificate for, whose host name and port are the
    /// listener's.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The server has no port.</exception>
    /// <exception cref="PluginContractViolationException">
    /// A client accepted the certificate, or failed for a reason other than refusing it.
    /// </exception>
    public static async Task AssertSelfSignedIsRefusedByDefaultAsync(
        IPanelTransport transport,
        PanelServerConnection serverWithSettingOff,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(serverWithSettingOff);
        var port = PortOf(serverWithSettingOff);

        if (await ReachesAsync(transport.CreateClient(), serverWithSettingOff.Hostname, port, ct))
        {
            throw new PluginContractViolationException(
                "CreateClient() accepted a self signed certificate; trust must never be on for every request.");
        }

        if (await ReachesAsync(transport.CreateClient(serverWithSettingOff), serverWithSettingOff.Hostname, port, ct))
        {
            throw new PluginContractViolationException(
                "CreateClient(server) accepted a self signed certificate for a server whose operator setting is off.");
        }
    }

    /// <summary>
    /// Asserts that the per server client of a server the operator allowed a self signed certificate for reaches the
    /// listener at the server's host name and port, and refuses the same certificate at another port of the same host
    /// and at another name of the same address, so the allowance applies to that one host name and port only. The plain
    /// client must still refuse it too.
    /// </summary>
    /// <param name="transport">The transport under test.</param>
    /// <param name="serverWithSettingOn">
    /// A server whose host name and port are the listener's and whose setting is on.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The server has no port.</exception>
    /// <exception cref="PluginContractViolationException">
    /// The per server client refused the certificate where it is allowed, accepted it elsewhere, or a client failed
    /// for a reason other than refusing it.
    /// </exception>
    public static async Task AssertSelfSignedIsAcceptedForTheServerOnlyAsync(
        IPanelTransport transport,
        PanelServerConnection serverWithSettingOn,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(serverWithSettingOn);
        var port = PortOf(serverWithSettingOn);
        var host = serverWithSettingOn.Hostname;

        if (!await ReachesAsync(transport.CreateClient(serverWithSettingOn), host, port, ct))
        {
            throw new PluginContractViolationException(
                "CreateClient(server) refused the self signed certificate of a server the operator allowed it for.");
        }

        await using var other = SelfSignedListener.Start();
        if (await ReachesAsync(transport.CreateClient(serverWithSettingOn), host, other.Port, ct))
        {
            throw new PluginContractViolationException(
                "CreateClient(server) accepted a self signed certificate at another port of the same host; the " +
                "allowance applies to the server's own port only.");
        }

        var otherName = string.Equals(host, SelfSignedListener.HostName, StringComparison.OrdinalIgnoreCase)
            ? SelfSignedListener.LoopbackAddress
            : SelfSignedListener.HostName;
        if (await ReachesAsync(transport.CreateClient(serverWithSettingOn), otherName, port, ct))
        {
            throw new PluginContractViolationException(
                "CreateClient(server) accepted a self signed certificate for another host name of the same " +
                "address; the allowance applies to the server's own host name only.");
        }

        if (await ReachesAsync(transport.CreateClient(), host, port, ct))
        {
            throw new PluginContractViolationException(
                "CreateClient() accepted a self signed certificate; the allowance of one server must not reach " +
                "other requests.");
        }
    }

    /// <summary>Reads the port of a server and refuses one without a port.</summary>
    /// <param name="server">The server.</param>
    /// <returns>The port.</returns>
    /// <exception cref="ArgumentException">The server has no port.</exception>
    private static int PortOf(PanelServerConnection server)
    {
        return server.Port ?? throw new ArgumentException("The server needs the port of the listener.", nameof(server));
    }

    /// <summary>
    /// Sends one request and tells whether it succeeded. A refusal of the certificate answers
    /// <see langword="false"/>; any other failure is a broken check and not a refusal, so it is reported as one.
    /// </summary>
    /// <param name="client">The client to try.</param>
    /// <param name="host">The host name to call.</param>
    /// <param name="port">The port to call.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when the listener answered; <see langword="false"/> when the client refused it.</returns>
    /// <exception cref="PluginContractViolationException">The request failed for a reason other than the certificate.</exception>
    private static async Task<bool> ReachesAsync(HttpClient client, string host, int port, CancellationToken ct)
    {
        using (client)
        {
            try
            {
                using var response = await client.GetAsync($"https://{FormatHost(host)}:{port}/", ct);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex) when (ex.InnerException is AuthenticationException)
            {
                // The TLS handshake failed on the certificate, which is the refusal the checks look for.
                return false;
            }
            catch (HttpRequestException)
            {
                throw new PluginContractViolationException(
                    "A client failed for a reason other than the certificate (the address may be blocked or the " +
                    "listener unreachable), so the check says nothing about trust.");
            }
        }
    }

    /// <summary>Puts an IPv6 literal in the brackets an address needs; any other host name is unchanged.</summary>
    /// <param name="host">The host name or address.</param>
    /// <returns>The host as it is written in an address.</returns>
    private static string FormatHost(string host)
    {
        return host.Contains(':') && !host.StartsWith('[') ? $"[{host}]" : host;
    }
}
