namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>One choice of a select setting.</summary>
public sealed class PluginSettingOption
{
    /// <summary>Gets the value stored when this option is chosen.</summary>
    public required string Value { get; init; }

    /// <summary>Gets the text shown for the option: a key of the manifest locales, or plain text.</summary>
    public required string Label { get; init; }
}
