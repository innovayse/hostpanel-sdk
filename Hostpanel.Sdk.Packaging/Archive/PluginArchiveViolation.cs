using Hostpanel.Sdk.Packaging.Enums;

namespace Hostpanel.Sdk.Packaging.Archive;

/// <summary>One guard a plugin archive fails, as <see cref="PluginArchiveGuard"/> reports it.</summary>
/// <param name="Rule">The guard that failed.</param>
/// <param name="Detail">What was found, for a log: counts and limits, never an absolute path.</param>
public sealed record PluginArchiveViolation(PluginArchiveRule Rule, string Detail);
