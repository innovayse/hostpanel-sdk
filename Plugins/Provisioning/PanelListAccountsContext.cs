namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>Input for <see cref="IProvisioningPanelPlugin.ListAccountsAsync"/>.</summary>
/// <param name="Server">The server whose accounts are listed.</param>
/// <param name="Cursor">The <see cref="PanelAccountListResult.NextCursor"/> of the previous page, or <see langword="null"/> for the first page.</param>
public sealed record PanelListAccountsContext(PanelServerConnection Server, string? Cursor);
