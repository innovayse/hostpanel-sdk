namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// The kind of registrant a set of domain fields applies to, written in the <c>holder</c> member of a <c>domainFields</c>
/// entry of <c>plugin.json</c> (camelCase). Values start at 1; 0 is not a holder.
/// </summary>
public enum PluginDomainHolder
{
    /// <summary>Every registrant. As the holder of a set it applies to a person and an organisation alike.</summary>
    Any = 1,

    /// <summary>A private person.</summary>
    Individual = 2,

    /// <summary>A company or other organisation.</summary>
    Organization = 3,
}
