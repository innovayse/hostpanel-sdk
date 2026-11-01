using System.Net;
using System.Net.Sockets;

namespace Innovayse.SDK.Plugins.HostingPanel;

/// <summary>Builds the address a hosting panel answers on, so every provider spells it the same way.</summary>
public static class HostingPanelAddress
{
    /// <summary>Builds the HTTPS base URL of a panel endpoint, without a trailing slash.</summary>
    /// <param name="host">The panel server's hostname or IP address. An IPv6 address is written in brackets in the URL.</param>
    /// <param name="port">The port the endpoint answers on.</param>
    /// <returns>The URL, e.g. <c>https://panel.example.com:2304</c> or <c>https://[2001:db8::1]:2304</c>.</returns>
    public static string BaseUrl(string host, int port)
    {
        // A bare IPv6 literal contains colons, which a URL would read as the start of the port; only a
        // literal that is not bracketed yet is wrapped, so a caller that already brackets it is unchanged.
        var needsBrackets = !host.StartsWith('[')
            && IPAddress.TryParse(host, out var address)
            && address.AddressFamily == AddressFamily.InterNetworkV6;

        return needsBrackets ? $"https://[{host}]:{port}" : $"https://{host}:{port}";
    }
}
