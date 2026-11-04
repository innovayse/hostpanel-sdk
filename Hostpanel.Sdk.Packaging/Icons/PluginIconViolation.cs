using Hostpanel.Sdk.Packaging.Enums;

namespace Hostpanel.Sdk.Packaging.Icons;

/// <summary>One icon rule a plugin breaks, as <see cref="PluginIconChecker"/> reports it.</summary>
/// <param name="Rule">The rule that is broken.</param>
/// <param name="Detail">What is wrong, naming the package-relative path only: never an absolute path or an exception message.</param>
public sealed record PluginIconViolation(PluginIconRule Rule, string Detail);
