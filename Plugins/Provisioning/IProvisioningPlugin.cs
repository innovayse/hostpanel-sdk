namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Marker interface for all provisioning provider plugins.
/// Implement via <see cref="Base.ProvisioningProviderBase"/>. The marker has no operations and stays unchanged so a plugin
/// built on 3.0.0 still compiles; a hosting control panel also implements <see cref="IProvisioningPanelPlugin"/>, which
/// carries the account operations.
/// </summary>
public interface IProvisioningPlugin { }
