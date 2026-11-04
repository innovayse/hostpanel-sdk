using Hostpanel.Sdk.Packaging.Enums;

namespace Hostpanel.Sdk.Packaging.Manifest;

/// <summary>One manifest rule a plugin breaks, as <see cref="PluginManifestChecker"/> reports it.</summary>
/// <param name="Rule">The rule that is broken.</param>
/// <param name="Detail">Where and how the rule is broken, for a log: the member of the manifest and its value.</param>
public sealed record PluginManifestViolation(PluginManifestRule Rule, string Detail);
