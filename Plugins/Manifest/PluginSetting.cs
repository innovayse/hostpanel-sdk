using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>
/// One typed setting an administrator fills in for a plugin, an entry of <c>settings</c> in <c>plugin.json</c>.
/// It supersedes <see cref="PluginField"/>; a legacy field converts with <see cref="FromField"/>.
/// </summary>
public sealed class PluginSetting
{
    /// <summary>Backing field of <see cref="Options"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginSettingOption> _options = [];

    /// <summary>Gets the settings-table key of this setting.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the text shown for the setting: a key of the manifest locales, or plain text.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the input kind.</summary>
    public required PluginSettingType Type { get; init; }

    /// <summary>Gets a value indicating whether the setting must be non-empty before the plugin can be enabled.</summary>
    public bool Required { get; init; }

    /// <summary>
    /// Gets the choices of a <see cref="PluginSettingType.Select"/> setting; empty for every other kind. A missing or
    /// <c>null</c> <c>options</c> in the JSON reads as an empty list, never <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<PluginSettingOption> Options
    {
        get => _options;
        init => _options = value ?? [];
    }

    /// <summary>Converts a legacy <see cref="PluginField"/> into the setting that replaces it.</summary>
    /// <param name="field">The field from the <c>fields</c> member of <c>plugin.json</c>.</param>
    /// <returns>
    /// The setting with the same key, label and requirement. Its type is the field type read without regard
    /// to case: <c>secret</c> and <c>password</c> become <see cref="PluginSettingType.Secret"/>, <c>select</c>,
    /// <c>textarea</c>, <c>url</c> and <c>number</c> become their own kind, <c>boolean</c> and <c>toggle</c> become
    /// <see cref="PluginSettingType.Boolean"/>, and anything else becomes <see cref="PluginSettingType.Text"/>.
    /// A legacy select has no options.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <see langword="null"/>.</exception>
    public static PluginSetting FromField(PluginField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        return new PluginSetting
        {
            Key = field.Key,
            Label = field.Label,
            Type = ReadFieldType(field.Type),
            Required = field.Required,
        };
    }

    /// <summary>Reads the free-text type of a legacy field into a setting kind.</summary>
    /// <param name="fieldType">The field type, possibly <see langword="null"/> in a malformed manifest.</param>
    /// <returns>The matching kind, or <see cref="PluginSettingType.Text"/> when none matches.</returns>
    private static PluginSettingType ReadFieldType(string? fieldType)
    {
        return fieldType?.ToLowerInvariant() switch
        {
            "secret" or "password" => PluginSettingType.Secret,
            "select" => PluginSettingType.Select,
            "textarea" => PluginSettingType.Textarea,
            "url" => PluginSettingType.Url,
            "number" => PluginSettingType.Number,
            "boolean" or "toggle" => PluginSettingType.Boolean,
            _ => PluginSettingType.Text,
        };
    }
}
