using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>One way a set of extended attributes breaks the domain fields it is checked against.</summary>
/// <param name="Key">The key of the offending attribute: the declared field key, or the unknown key that was sent.</param>
/// <param name="Reason">Why the value is refused.</param>
public sealed record PluginDomainFieldViolation(string Key, PluginDomainFieldViolationReason Reason);
