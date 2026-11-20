namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>Input for <see cref="IProvisioningPanelPlugin.ChangePackageAsync"/>.</summary>
/// <param name="Server">The server the account lives on.</param>
/// <param name="Service">The service and its account; <see cref="PanelService.ProvisioningRef"/> is set.</param>
/// <param name="Package">The package the account moves to.</param>
public sealed record PanelChangePackageContext(PanelServerConnection Server, PanelService Service, PanelPackage Package)
{
    /// <summary>
    /// Gets the per product choices the host stores next to the package, keyed by the names the plugin documents (for
    /// example a region or a backup option on a virtual server platform), or <see langword="null"/> when the product has none.
    /// Never a secret. A plugin ignores a key it does not know. Optional and null by default, so existing plugins and hosts are unchanged.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ProductOptions { get; init; }
}
