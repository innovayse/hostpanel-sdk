using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>
/// The extra fields one group of zones needs, an entry of <c>domainFields</c> in <c>plugin.json</c> (manifest version 2,
/// registrar plugins only). <see cref="PluginDomainFieldRules.Match"/> picks the set that applies to a domain.
/// </summary>
public sealed class PluginDomainFieldSet
{
    /// <summary>Backing field of <see cref="Zones"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<string> _zones = [];

    /// <summary>Backing field of <see cref="Fields"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginDomainField> _fields = [];

    /// <summary>
    /// Gets the zones the set applies to: lower case ASCII suffixes without a leading dot, a top level domain such as
    /// <c>com</c>, a second level one such as <c>co.uk</c> or a punycode one such as <c>xn--p1ai</c>. A domain belongs to a
    /// zone when it ends with a dot and the zone; when several sets name zones that fit a domain the longest zone wins. At
    /// least one zone is needed. A missing or <c>null</c> <c>zones</c> reads as an empty list, never <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<string> Zones
    {
        get => _zones;
        init => _zones = value ?? [];
    }

    /// <summary>Gets the kind of registrant the set applies to; <see cref="PluginDomainHolder.Any"/> by default.</summary>
    public PluginDomainHolder Holder { get; init; } = PluginDomainHolder.Any;

    /// <summary>Gets the operations the set applies to; <see cref="PluginDomainOperations.All"/> by default.</summary>
    public PluginDomainOperations Operations { get; init; } = PluginDomainOperations.All;

    /// <summary>
    /// Gets the fields to ask for. A set without fields is allowed: it states that its zones need none, which overrides
    /// the fields of a shorter zone. A missing or <c>null</c> <c>fields</c> reads as an empty list, never <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<PluginDomainField> Fields
    {
        get => _fields;
        init => _fields = value ?? [];
    }
}
