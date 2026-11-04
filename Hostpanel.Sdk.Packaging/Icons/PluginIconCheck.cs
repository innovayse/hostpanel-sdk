namespace Hostpanel.Sdk.Packaging.Icons;

/// <summary>The outcome of <see cref="PluginIconChecker.Check(string, string?)"/>: a checked icon, no icon, or the rule the icon breaks.</summary>
/// <param name="Icon">The checked icon; <see langword="null"/> when the manifest names none or the icon is refused.</param>
/// <param name="Violation">The rule the icon breaks; <see langword="null"/> when the icon is accepted or absent.</param>
public sealed record PluginIconCheck(PluginIcon? Icon, PluginIconViolation? Violation);
