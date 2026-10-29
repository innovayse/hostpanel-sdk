namespace Innovayse.SDK.Plugins;

/// <summary>Builds the address a hosting panel answers on, so every provider spells it the same way.</summary>
public static class HostingPanelAddress
{
    /// <summary>Builds the HTTPS base URL of a panel endpoint, without a trailing slash.</summary>
    /// <param name="host">The panel server's hostname or IP address.</param>
    /// <param name="port">The port the endpoint answers on.</param>
    /// <returns>The URL, e.g. <c>https://panel.example.com:2304</c>.</returns>
    public static string BaseUrl(string host, int port) => $"https://{host}:{port}";
}
