namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// The resource figures a panel reports for one account. Every figure is optional: a panel reports what it has, and the
/// host shows only those. Sizes are in bytes, so no unit is ever guessed. The inode, mailbox, database and domain figures
/// are init members rather than constructor parameters, so a plugin built on an earlier version that calls the constructor
/// still loads; a plugin sets them with an object initializer.
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
    DateTimeOffset ReadAt)
{
    /// <summary>Gets the number of inodes (files and directories) the account uses, or <see langword="null"/> when the panel does not report it. Optional and null by default, so existing plugins and hosts are unchanged.</summary>
    public long? InodesUsed { get; init; }

    /// <summary>Gets the inode quota, or <see langword="null"/> when the account has none or the panel does not report it.</summary>
    public long? InodesLimit { get; init; }

    /// <summary>Gets the number of mailboxes the account has, or <see langword="null"/> when the panel does not report it.</summary>
    public int? MailboxesUsed { get; init; }

    /// <summary>Gets the mailbox quota, or <see langword="null"/> when the account has none or the panel does not report it.</summary>
    public int? MailboxesLimit { get; init; }

    /// <summary>Gets the number of databases the account has, or <see langword="null"/> when the panel does not report it.</summary>
    public int? DatabasesUsed { get; init; }

    /// <summary>Gets the database quota, or <see langword="null"/> when the account has none or the panel does not report it.</summary>
    public int? DatabasesLimit { get; init; }

    /// <summary>Gets the number of domains (addon, parked and sub domains together, as the panel counts them) the account has, or <see langword="null"/> when the panel does not report it.</summary>
    public int? DomainsUsed { get; init; }

    /// <summary>Gets the domain quota, or <see langword="null"/> when the account has none or the panel does not report it.</summary>
    public int? DomainsLimit { get; init; }
}
