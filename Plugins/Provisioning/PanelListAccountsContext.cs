using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>Input for <see cref="IProvisioningPanelPlugin.ListAccountsAsync"/>.</summary>
/// <param name="Server">The server whose accounts are listed.</param>
/// <param name="Cursor">The <see cref="PanelAccountListResult.NextCursor"/> of the previous page, or <see langword="null"/> for the first page.</param>
public sealed record PanelListAccountsContext(PanelServerConnection Server, string? Cursor)
{
    /// <summary>
    /// Gets the kind of account to list, or <see langword="null"/> for the panel's default, ordinary user accounts. Following
    /// the cursors lists the accounts of one level only. Optional and null by default, so existing plugins and hosts are unchanged.
    /// </summary>
    public PanelAccountLevel? Level { get; init; }
}
