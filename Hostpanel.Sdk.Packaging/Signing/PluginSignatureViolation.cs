using Hostpanel.Sdk.Packaging.Enums;

namespace Hostpanel.Sdk.Packaging.Signing;

/// <summary>One signature check a plugin package fails, as <see cref="PluginSignatureValidator"/> reports it.</summary>
/// <param name="Rule">The check that failed.</param>
/// <param name="Detail">What was found, for a log: no absolute path of the machine it ran on.</param>
public sealed record PluginSignatureViolation(PluginSignatureRule Rule, string Detail);
