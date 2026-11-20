using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// Input for the operations that act on one existing restore point: the restore and the delete of a snapshot
/// (<see cref="IVirtualServerSnapshotPlugin"/>) or of a backup (<see cref="IVirtualServerBackupPlugin"/>).
/// </summary>
/// <param name="Server">The platform the virtual server lives on.</param>
/// <param name="Service">The service and its virtual server; <see cref="PanelService.ProvisioningRef"/> is set.</param>
/// <param name="RestorePointId">The id of the restore point, as the listing returned it in <see cref="VirtualServerRestorePoint.Id"/>.</param>
public sealed record VirtualServerRestorePointContext(PanelServerConnection Server, PanelService Service, string RestorePointId);
