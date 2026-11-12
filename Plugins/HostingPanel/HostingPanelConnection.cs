using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.HostingPanel;

/// <summary>Where and how to reach a hosting panel's REST API.</summary>
/// <param name="Host">The panel server's hostname or IP address.</param>
/// <param name="Port">The port the panel answers its REST API on.</param>
/// <param name="ApiKey">The API key the panel authenticates the call with.</param>
[Obsolete("Superseded by Hostpanel.Sdk.Plugins.Provisioning.IProvisioningPanelPlugin: a panel plugin implements it and reaches its panel through IPanelTransport. This type will be removed in 4.0.0.")]
public sealed partial record HostingPanelConnection(string Host, int Port, [property: SecretMember] string ApiKey)
{
    /// <summary>Gets the HTTPS base URL of the panel's REST API.</summary>
    public string BaseUrl => HostingPanelAddress.BaseUrl(Host, Port);
}
