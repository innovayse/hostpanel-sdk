namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// A hosting package a panel offers, with the limits the panel reports for it, so the admin who builds a product can pick one
/// and see what it gives. Every limit is optional: a panel reports what it has, and the host shows only those. Sizes are in
/// bytes, so no unit is ever guessed. A <see langword="null"/> limit means the package has none or the panel does not
/// report it.
/// </summary>
/// <param name="Name">The package name exactly as the panel knows it; the host sends it back in <see cref="PanelPackage.Name"/>. Not empty, and unique in one listing.</param>
/// <param name="DiskLimitBytes">The disk quota, or <see langword="null"/>.</param>
/// <param name="BandwidthLimitBytes">The traffic quota of the panel's accounting period, or <see langword="null"/>.</param>
/// <param name="InodesLimit">The inode quota, or <see langword="null"/>.</param>
/// <param name="MailboxesLimit">The number of mailboxes the package allows, or <see langword="null"/>.</param>
/// <param name="DatabasesLimit">The number of databases the package allows, or <see langword="null"/>.</param>
/// <param name="DomainsLimit">The number of domains the package allows, or <see langword="null"/>.</param>
public sealed record PanelPackageDetails(
    string Name,
    long? DiskLimitBytes,
    long? BandwidthLimitBytes,
    long? InodesLimit,
    int? MailboxesLimit,
    int? DatabasesLimit,
    int? DomainsLimit);
