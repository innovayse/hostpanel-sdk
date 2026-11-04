namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>Input for <see cref="IProvisioningPanelPlugin.TestConnectionAsync"/>.</summary>
/// <param name="Server">The server to test.</param>
public sealed record PanelTestConnectionContext(PanelServerConnection Server);
