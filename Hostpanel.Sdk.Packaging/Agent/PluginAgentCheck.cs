namespace Hostpanel.Sdk.Packaging.Agent;

/// <summary>The outcome of <see cref="PluginAgentPackageChecker.Check(string, string?)"/>: the checked files, nothing to check, or the rule the package breaks.</summary>
/// <param name="Files">The checked files; <see langword="null"/> when the manifest names no profile or the package is refused.</param>
/// <param name="Violation">The rule the package breaks; <see langword="null"/> when the files are accepted or none are needed.</param>
public sealed record PluginAgentCheck(PluginAgentFiles? Files, PluginAgentViolation? Violation);
