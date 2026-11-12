using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Checks a plugin author runs in the plugin's own tests, with a <see cref="SelfSignedListener"/>, to prove that a
/// transport keeps the promise of <see cref="IPanelTransport.CreateClient(PanelServerConnection)"/>: the choice to trust
/// a self signed certificate belongs to the operator and applies to one server only. Each check throws
/// <see cref="PluginContractViolationException"/> naming the promise that was broken. Nothing here depends on a test
/// framework.
/// </summary>
public static class PanelTransportContract
{
    /// <summary>
    /// Asserts that a client that was not created for a server with the operator's setting refuses the self signed
    /// certificate: both <see cref="IPanelTransport.CreateClient()"/> and the per server client of a server whose
    /// setting is off must fail the TLS handshake, so trust is never global and never on by default.
    /// </summary>
    /// <param name="transport">The transport under test.</param>
    /// <param name="listener">The listener with the self signed certificate.</param>
    /// <param name="serverWithSettingOff">A server the operator did not allow a self signed certificate for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">A client accepted the certificate.</exception>
    public static async Task AssertSelfSignedIsRefusedByDefaultAsync(
        IPanelTransport transport,
        SelfSignedListener listener,
        PanelServerConnection serverWithSettingOff,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(serverWithSettingOff);

        if (await ReachesAsync(transport.CreateClient(), listener, ct))
        {
            throw new PluginContractViolationException(
                "CreateClient() accepted a self signed certificate; trust must never be on for every request.");
        }

        if (await ReachesAsync(transport.CreateClient(serverWithSettingOff), listener, ct))
        {
            throw new PluginContractViolationException(
                "CreateClient(server) accepted a self signed certificate for a server whose operator setting is off.");
        }
    }

    /// <summary>
    /// Asserts that the per server client of a server the operator allowed a self signed certificate for reaches the
    /// listener, and that the plain client still refuses it, so the allowance applies to that one server only.
    /// </summary>
    /// <param name="transport">The transport under test.</param>
    /// <param name="listener">The listener with the self signed certificate.</param>
    /// <param name="serverWithSettingOn">A server whose host name and port are the listener's and whose setting is on.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">
    /// The per server client refused the certificate, or the plain client accepted it.
    /// </exception>
    public static async Task AssertSelfSignedIsAcceptedForTheServerOnlyAsync(
        IPanelTransport transport,
        SelfSignedListener listener,
        PanelServerConnection serverWithSettingOn,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(serverWithSettingOn);

        if (!await ReachesAsync(transport.CreateClient(serverWithSettingOn), listener, ct))
        {
            throw new PluginContractViolationException(
                "CreateClient(server) refused the self signed certificate of a server the operator allowed it for.");
        }

        if (await ReachesAsync(transport.CreateClient(), listener, ct))
        {
            throw new PluginContractViolationException(
                "CreateClient() accepted a self signed certificate; the allowance of one server must not reach other requests.");
        }
    }

    /// <summary>Sends one request to the listener and tells whether the TLS handshake and the request succeeded.</summary>
    /// <param name="client">The client to try.</param>
    /// <param name="listener">The listener with the self signed certificate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when the listener answered; <see langword="false"/> when the client refused it.</returns>
    private static async Task<bool> ReachesAsync(HttpClient client, SelfSignedListener listener, CancellationToken ct)
    {
        using (client)
        {
            try
            {
                using var response = await client.GetAsync(
                    $"https://{SelfSignedListener.HostName}:{listener.Port}/", ct);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                // A client that refuses the certificate fails the handshake, which is the answer being looked for.
                return false;
            }
        }
    }
}
