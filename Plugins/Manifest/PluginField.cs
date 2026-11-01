namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>
/// Describes a single configuration field exposed by a plugin.
/// </summary>
public sealed class PluginField
{
    /// <summary>Gets the settings-table key for this field.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the human-readable label shown in the admin UI.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the field input type: text | secret | select | textarea.</summary>
    public required string Type { get; init; }

    /// <summary>Gets a value indicating whether this field must be non-empty before the integration can be enabled.</summary>
    public bool Required { get; init; }
}
