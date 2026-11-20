using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// The snapshots of a virtual server, as a separate opt in: a virtual server plugin implements it next to
/// <see cref="IVirtualServerPlugin"/> and lists <see cref="PluginCapabilities.VirtualServerSnapshots"/> in its manifest. A host
/// treats a plugin that does not implement it as <see cref="PanelOperationOutcome.NotSupported"/>. It is a separate interface
/// because a new operation always arrives as a new interface, never as a member of an existing one.
/// </summary>
/// <remarks>
/// The rules of <see cref="IVirtualServerPlugin"/> hold here: the platform arrives with its stored credentials, a refusal, a
/// missing server and a missing snapshot are outcomes and never exceptions, a repeat ends in the same state as the first call,
/// and the platform's own text is never returned. A snapshot the platform makes in the background is answered
/// <see cref="PanelOperationOutcome.Pending"/> until it exists.
/// </remarks>
public interface IVirtualServerSnapshotPlugin : IVirtualServerPlugin
{
    /// <summary>Lists the snapshots of a server. Read only; safe to repeat. Every entry has an id and a name that are not empty, and no id appears twice.</summary>
    /// <param name="context">The platform and the server.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the snapshots (an empty list is a server without snapshots), <c>NotFound</c> for a server the platform does not hold, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<IReadOnlyList<VirtualServerRestorePoint>, PanelRefusalReason>> ListSnapshotsAsync(PanelAccountContext context, CancellationToken ct);

    /// <summary>
    /// Takes a snapshot of the server as it is now. Idempotent on <see cref="VirtualServerRestorePointCreateContext.Name"/>: a
    /// snapshot the platform already holds under that name is answered <see cref="PanelOperationOutcome.AlreadyApplied"/> and is
    /// neither made again nor changed.
    /// </summary>
    /// <param name="context">The platform, the server and the name of the snapshot.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>Pending</c> while the platform makes it, <c>NotFound</c> or <c>Refused</c> (<see cref="PanelRefusalReason.LimitReached"/> when the platform allows no more).</returns>
    Task<PanelActionResult> CreateSnapshotAsync(VirtualServerRestorePointCreateContext context, CancellationToken ct);

    /// <summary>
    /// Returns the server to a snapshot. The server's disk is replaced, so the host sends it only on the client's explicit
    /// request. A repeat sets the same state again and answers <c>Succeeded</c>.
    /// </summary>
    /// <param name="context">The platform, the server and the id of the snapshot.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>Pending</c> while the platform restores it, <c>NotFound</c> (the server or the snapshot is not held) or <c>Refused</c>.</returns>
    Task<PanelActionResult> RestoreSnapshotAsync(VirtualServerRestorePointContext context, CancellationToken ct);

    /// <summary>
    /// Deletes a snapshot. Idempotent: a snapshot that is already gone is <see cref="PanelOperationOutcome.AlreadyApplied"/>,
    /// because the wanted state, no snapshot, is reached; the plugin never answers <c>NotFound</c> for it.
    /// </summary>
    /// <param name="context">The platform, the server and the id of the snapshot.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>Pending</c> while the platform deletes it, <c>NotFound</c> for a server the platform does not hold, or <c>Refused</c>.</returns>
    Task<PanelActionResult> DeleteSnapshotAsync(VirtualServerRestorePointContext context, CancellationToken ct);
}
