namespace Hostpanel.Sdk.Plugins.HostingPanel;

/// <summary>Abstraction over the hosting panel (v7) REST API client for server info operations.</summary>
[Obsolete("Superseded by Hostpanel.Sdk.Plugins.Provisioning.IProvisioningPanelPlugin: a panel plugin implements it and reaches its panel through IPanelTransport. This type will be removed in 4.0.0.")]
public interface IHostingPanelV7ApiClient
{
    /// <summary>Fetches server metadata by listing packages to verify connectivity.</summary>
    /// <param name="connection">Where and how to reach the panel's API.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The package count the panel reports.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    Task<HostingPanelV7ServerInfo> GetServerInfoAsync(HostingPanelConnection connection, CancellationToken ct);
}
