namespace Innovayse.SDK.Plugins;

/// <summary>Where and how to reach a hosting panel's REST API.</summary>
/// <param name="Host">The panel server's hostname or IP address.</param>
/// <param name="Port">The port the panel answers its REST API on.</param>
/// <param name="ApiKey">The API key the panel authenticates the call with.</param>
public sealed record HostingPanelConnection(string Host, int Port, string ApiKey)
{
    /// <summary>Gets the HTTPS base URL of the panel's REST API.</summary>
    public string BaseUrl => HostingPanelAddress.BaseUrl(Host, Port);
}
