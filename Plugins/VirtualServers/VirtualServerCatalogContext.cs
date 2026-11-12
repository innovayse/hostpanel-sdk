using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>Input for <see cref="IVirtualServerPlugin.ListImagesAsync"/> and <see cref="IVirtualServerPlugin.ListPlansAsync"/>.</summary>
/// <param name="Server">The platform whose catalogue is wanted.</param>
public sealed record VirtualServerCatalogContext(PanelServerConnection Server);
