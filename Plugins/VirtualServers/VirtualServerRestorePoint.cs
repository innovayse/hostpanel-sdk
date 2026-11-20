namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// A state of a virtual server the platform keeps so the server can return to it: a snapshot or a backup, as
/// <see cref="IVirtualServerSnapshotPlugin"/> and <see cref="IVirtualServerBackupPlugin"/> list them.
/// </summary>
/// <param name="Id">The platform's id of the restore point, which the host sends back in <see cref="VirtualServerRestorePointContext.RestorePointId"/>. Not empty, and unique in one listing.</param>
/// <param name="Name">The name of the restore point: the one <see cref="VirtualServerRestorePointCreateContext.Name"/> asked for, or the platform's own for one it made itself. Not empty.</param>
/// <param name="CreatedAt">When the platform made the restore point, or <see langword="null"/> when it does not say.</param>
/// <param name="SizeBytes">The space the restore point takes, in bytes, or <see langword="null"/> when the platform does not say.</param>
public sealed record VirtualServerRestorePoint(string Id, string Name, DateTimeOffset? CreatedAt, long? SizeBytes);
