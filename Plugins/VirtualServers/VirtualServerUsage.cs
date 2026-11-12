using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// The figures a platform reports for one virtual server. Every figure is optional: a platform reports what it has, and the
/// host shows only those. Sizes are in bytes, so no unit is ever guessed.
/// </summary>
/// <param name="State">The power state, or <see langword="null"/> when the platform does not say.</param>
/// <param name="CpuPercent">The processor load as a percentage of what the plan gives, 0 to 100, or <see langword="null"/> when the platform does not report it.</param>
/// <param name="MemoryUsedBytes">The memory in use, or <see langword="null"/> when the platform does not report it.</param>
/// <param name="MemoryLimitBytes">The memory the plan gives, or <see langword="null"/> when the platform does not report it.</param>
/// <param name="DiskUsedBytes">The disk space in use, or <see langword="null"/> when the platform does not report it.</param>
/// <param name="DiskLimitBytes">The disk size the plan gives, or <see langword="null"/> when the platform does not report it.</param>
/// <param name="BandwidthUsedBytes">The traffic used in the platform's current accounting period, or <see langword="null"/> when the platform does not report it.</param>
/// <param name="BandwidthLimitBytes">The traffic quota of that period, or <see langword="null"/> when the plan has none or the platform does not report it.</param>
/// <param name="ReadAt">When the plugin read the figures from the platform.</param>
public sealed record VirtualServerUsage(
    VirtualServerPowerState? State,
    double? CpuPercent,
    long? MemoryUsedBytes,
    long? MemoryLimitBytes,
    long? DiskUsedBytes,
    long? DiskLimitBytes,
    long? BandwidthUsedBytes,
    long? BandwidthLimitBytes,
    DateTimeOffset ReadAt);
