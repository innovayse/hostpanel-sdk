namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>The hosting package an account is created on or moved to.</summary>
/// <param name="Name">The package name exactly as it is configured on the panel; the host takes it from the product and does not translate it.</param>
public sealed record PanelPackage(string Name);
