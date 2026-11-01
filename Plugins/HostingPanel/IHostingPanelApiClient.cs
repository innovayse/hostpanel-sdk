namespace Innovayse.SDK.Plugins.HostingPanel;

/// <summary>Abstraction over the hosting panel REST API client for server info operations.</summary>
public interface IHostingPanelApiClient
{
    /// <summary>
    /// The port a hosting panel server answers its REST API on when a deployment has not configured one.
    /// </summary>
    /// <remarks>
    /// Declared on the contract rather than on an implementation because the code that applies the "no port
    /// configured" fallback and the client that uses the port must agree on one number, and a client is loaded
    /// reflectively, so the caller cannot reference a constant inside it.
    /// <para>
    /// <b>2304, not 2031.</b> 2031 is the panel's own HTTPS user interface; it answers nothing under
    /// <c>/v1/</c>, which is the only path anything built from this constant requests. A fallback to 2031 makes
    /// a deployment that has configured a host and an API key but no port probe the user interface and report
    /// the server as unreachable.
    /// </para>
    /// </remarks>
    public const int DefaultApiPort = 2304;

    /// <summary>Fetches server metadata including account count and panel version.</summary>
    /// <param name="connection">Where and how to reach the panel's API.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The account count and version the panel reports.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    Task<HostingPanelServerInfo> GetServerInfoAsync(HostingPanelConnection connection, CancellationToken ct);
}
