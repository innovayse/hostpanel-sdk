using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// Input for <see cref="IVirtualServerSnapshotPlugin.CreateSnapshotAsync"/> and <see cref="IVirtualServerBackupPlugin.CreateBackupAsync"/>.
/// The host chooses <see cref="Name"/> and stores it before it calls the plugin, so a retry of the same request sends the
/// same name: that is what makes the call idempotent.
/// </summary>
/// <param name="Server">The platform the virtual server lives on.</param>
/// <param name="Service">The service and its virtual server; <see cref="PanelService.ProvisioningRef"/> is set.</param>
/// <param name="Name">The name of the restore point. Stable across retries of one request, and unique for the server.</param>
public sealed record VirtualServerRestorePointCreateContext(PanelServerConnection Server, PanelService Service, string Name);
