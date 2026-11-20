using System.Net;
using System.Net.Sockets;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Builds the base address a panel or platform plugin sends its requests to, from the <see cref="PanelServerConnection"/>
/// the host hands it, so every plugin spells the scheme, the port and an IPv6 literal the same way and none keeps its own
/// copy. The address is checked before it is built: a host name that carries a path, a query, credentials or white space
/// would send a request, and the credential on it, somewhere the operator never saved. The transport's address guard still
/// applies to whatever this returns; this class only refuses an address that is malformed.
/// </summary>
public static class PanelServerAddress
{
    /// <summary>The characters a host name never contains; each would end the host part of an address and start another part.</summary>
    private static readonly char[] ForbiddenHostCharacters = ['/', '\\', '?', '#', '@', '%', ' ', '\t', '\r', '\n'];

    /// <summary>
    /// Builds the base address of the server, without a trailing slash: the scheme from
    /// <see cref="PanelServerConnection.UseSsl"/>, the host name from <see cref="PanelServerConnection.Hostname"/> and the port
    /// from <see cref="PanelServerConnection.Port"/>, or <paramref name="defaultPort"/> when none is saved. An IPv6 literal is
    /// written in brackets, and one that already is stays as it is.
    /// </summary>
    /// <param name="server">The server the request is for.</param>
    /// <param name="defaultPort">The port the plugin's own panel answers on when the operator saved none.</param>
    /// <returns>The address, for example <c>https://panel.example.com:2087</c> or <c>http://[2001:db8::1]:8443</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="server"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="defaultPort"/> is not between 1 and 65535.</exception>
    /// <exception cref="ArgumentException">The saved host name is not a host name or an IP address, or the saved port is not between 1 and 65535.</exception>
    public static string BaseUrl(PanelServerConnection server, int defaultPort)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentOutOfRangeException.ThrowIfLessThan(defaultPort, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(defaultPort, IPEndPoint.MaxPort);

        var port = server.Port ?? defaultPort;
        if (port is < 1 or > IPEndPoint.MaxPort)
        {
            throw new ArgumentException("The saved port of the server is not between 1 and 65535.", nameof(server));
        }

        var scheme = server.UseSsl ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
        return $"{scheme}://{FormatHost(CheckedHost(server))}:{port}";
    }

    /// <summary>Builds the same address as <see cref="BaseUrl"/> as a <see cref="Uri"/>, for a plugin that resolves its paths against it.</summary>
    /// <param name="server">The server the request is for.</param>
    /// <param name="defaultPort">The port the plugin's own panel answers on when the operator saved none.</param>
    /// <returns>The address; its path is the root.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="server"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="defaultPort"/> is not between 1 and 65535.</exception>
    /// <exception cref="ArgumentException">The saved host name is not a host name or an IP address, or the saved port is not between 1 and 65535.</exception>
    public static Uri BaseUri(PanelServerConnection server, int defaultPort)
    {
        return new Uri(BaseUrl(server, defaultPort), UriKind.Absolute);
    }

    /// <summary>Puts an IPv6 literal in the brackets an address needs; a host name, an IPv4 address and a literal that is already bracketed are unchanged.</summary>
    /// <param name="host">The host name or address.</param>
    /// <returns>The host as it is written in an address.</returns>
    internal static string FormatHost(string host)
    {
        var needsBrackets = !host.StartsWith('[')
            && IPAddress.TryParse(host, out var address)
            && address.AddressFamily == AddressFamily.InterNetworkV6;
        return needsBrackets ? $"[{host}]" : host;
    }

    /// <summary>Checks that the saved host name of a server is a host name, an IPv4 address or an IPv6 address, bracketed or not.</summary>
    /// <param name="server">The server whose saved host name is checked.</param>
    /// <returns>The host name, unchanged.</returns>
    /// <exception cref="ArgumentException">The host name is empty or is not a host name or an IP address.</exception>
    private static string CheckedHost(PanelServerConnection server)
    {
        var hostname = server.Hostname;
        if (string.IsNullOrEmpty(hostname))
        {
            throw new ArgumentException("The saved host name of the server is not a host name or an IP address.", nameof(server));
        }

        var bracketed = hostname.Length > 2 && hostname[0] == '[' && hostname[^1] == ']';
        var literal = bracketed ? hostname[1..^1] : hostname;
        var kind = literal.IndexOfAny(ForbiddenHostCharacters) >= 0
            ? UriHostNameType.Unknown
            : Uri.CheckHostName(literal);

        // A bracketed host must be an IPv6 literal: brackets around a name or an IPv4 address are not an address.
        var valid = bracketed ? kind == UriHostNameType.IPv6 : kind is UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6;
        if (!valid)
        {
            throw new ArgumentException("The saved host name of the server is not a host name or an IP address.", nameof(server));
        }

        return hostname;
    }
}
