using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.HostingPanel;

/// <summary>Where and how to reach a hosting panel's REST API.</summary>
/// <param name="Host">The panel server's hostname or IP address.</param>
/// <param name="Port">The port the panel answers its REST API on.</param>
/// <param name="ApiKey">The API key the panel authenticates the call with.</param>
public sealed record HostingPanelConnection(string Host, int Port, string ApiKey)
{
    /// <summary>Gets the HTTPS base URL of the panel's REST API.</summary>
    public string BaseUrl => HostingPanelAddress.BaseUrl(Host, Port);

    /// <summary>Prints the record without its secret members, so the generated <c>ToString</c> never reveals them.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Host = ").Append(Host);
        builder.Append(", Port = ").Append(Port);
        builder.Append(", ApiKey = ").Append(SecretMask.Text);
        builder.Append(", BaseUrl = ").Append(BaseUrl);
        return true;
    }
}
