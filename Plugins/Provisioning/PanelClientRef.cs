namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>The client an account is created for, as far as a hosting panel needs to know them.</summary>
/// <param name="ClientReference">The host's stable reference for the client, opaque to the plugin.</param>
/// <param name="Email">The client's e-mail address, for the panel's contact field, or <see langword="null"/> when the host has none.</param>
/// <param name="Name">The client's display name, or <see langword="null"/> when the host has none.</param>
public sealed record PanelClientRef(string ClientReference, string? Email, string? Name);
