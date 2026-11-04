namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// The resource figures a panel reports for one account. Every figure is optional: a panel reports what it has, and the
/// host shows only those. Sizes are in bytes, so no unit is ever guessed.
/// </summary>
/// <param name="DiskUsedBytes">The disk space the account uses, or <see langword="null"/> when the panel does not report it.</param>
/// <param name="DiskLimitBytes">The disk quota, or <see langword="null"/> when the account has none or the panel does not report it.</param>
/// <param name="BandwidthUsedBytes">The traffic used in the panel's current accounting period, or <see langword="null"/> when the panel does not report it.</param>
/// <param name="BandwidthLimitBytes">The traffic quota of that period, or <see langword="null"/> when the account has none or the panel does not report it.</param>
/// <param name="ReadAt">When the plugin read the figures from the panel.</param>
public sealed record PanelUsage(
    long? DiskUsedBytes,
    long? DiskLimitBytes,
    long? BandwidthUsedBytes,
    long? BandwidthLimitBytes,
    DateTimeOffset ReadAt);
