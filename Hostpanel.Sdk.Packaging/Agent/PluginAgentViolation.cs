using Hostpanel.Sdk.Packaging.Enums;

namespace Hostpanel.Sdk.Packaging.Agent;

/// <summary>One agent rule a plugin package breaks, as <see cref="PluginAgentPackageChecker"/> reports it.</summary>
/// <param name="Rule">The rule that is broken.</param>
/// <param name="Detail">What is wrong, naming the package-relative path or the profile key only: never an absolute path, an exception message or a value taken from a file.</param>
public sealed record PluginAgentViolation(PluginAgentRule Rule, string Detail);
