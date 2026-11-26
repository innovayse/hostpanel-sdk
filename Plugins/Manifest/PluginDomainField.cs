using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>
/// One extra value a registry asks for beside the standard contact, an entry of <c>fields</c> in a
/// <see cref="PluginDomainFieldSet"/>. The host shows it as an input, validates the value with
/// <see cref="PluginDomainFieldRules.Validate"/> and passes it to the plugin in the <c>ExtendedAttributes</c> of the
/// registrar context under <see cref="Key"/>, unchanged.
/// </summary>
public sealed class PluginDomainField
{
    /// <summary>The most characters a <see cref="Key"/> may have.</summary>
    public const int MaxKeyLength = 64;

    /// <summary>The most characters a <see cref="Pattern"/> may have.</summary>
    public const int MaxPatternLength = 200;

    /// <summary>Backing field of <see cref="Options"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginSettingOption> _options = [];

    /// <summary>
    /// Gets the key of the field, the registry's own name for it, passed back unchanged as the key of the
    /// <c>ExtendedAttributes</c> entry. It is 1 to <see cref="MaxKeyLength"/> ASCII letters, digits, dots, underscores,
    /// colons and hyphens, starting with a letter or digit (see <see cref="PluginDomainFieldRules.IsValidKey"/>), and
    /// unique in the set that holds it.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>Gets the text shown for the field: a key of the manifest locales, or plain text.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the input kind.</summary>
    public required PluginDomainFieldType Type { get; init; }

    /// <summary>Gets a value indicating whether the field must have a value before the operation goes ahead.</summary>
    public bool Required { get; init; }

    /// <summary>Gets the help text shown under the field: a key of the manifest locales, or plain text; <see langword="null"/> for none.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the hint shown in an empty input: a key of the manifest locales, or plain text; <see langword="null"/> for none.</summary>
    public string? Placeholder { get; init; }

    /// <summary>
    /// Gets the regular expression the whole value must match, at most <see cref="MaxPatternLength"/> characters;
    /// <see langword="null"/> for none. It applies to every kind but <see cref="PluginDomainFieldType.Select"/>.
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>Gets the fewest characters of a value; <see langword="null"/> for no lower bound. It applies to every kind but <see cref="PluginDomainFieldType.Select"/>.</summary>
    public int? MinLength { get; init; }

    /// <summary>Gets the most characters of a value; <see langword="null"/> for no upper bound. It applies to every kind but <see cref="PluginDomainFieldType.Select"/>.</summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Gets the choices of a <see cref="PluginDomainFieldType.Select"/> field; empty for every other kind. A missing or
    /// <c>null</c> <c>options</c> in the JSON reads as an empty list, never <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<PluginSettingOption> Options
    {
        get => _options;
        init => _options = value ?? [];
    }
}
