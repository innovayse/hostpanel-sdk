using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// A console session on a virtual server. Whoever opens the address controls the server, so it is a credential: the
/// generated <c>ToString</c> prints it masked, and neither the plugin nor the host logs it.
/// </summary>
/// <param name="Url">The absolute, time limited address of the console (a VNC or web console). Not empty. Secret.</param>
/// <param name="ExpiresAt">When the address stops working, or <see langword="null"/> when the platform does not say.</param>
public sealed record VirtualServerConsole(string Url, DateTimeOffset? ExpiresAt)
{
    /// <summary>Prints the record without its secret member, so the generated <c>ToString</c> never reveals it.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Url = ").Append(SecretMask.Text);
        builder.Append(", ExpiresAt = ").Append(ExpiresAt);
        return true;
    }
}
