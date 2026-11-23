namespace Hostpanel.Sdk.Packaging.Agent;

/// <summary>The SSH key agent files of a plugin package that passed <see cref="PluginAgentPackageChecker"/>, kept as the bytes that were checked.</summary>
/// <param name="ProfileId">The profile id the manifest names, which is also the <c>provider</c> of the profile.</param>
/// <param name="Profile">The content of the profile file.</param>
/// <param name="Helpers">The content of each helper by file name, in ordinal order; empty when the package carries none.</param>
public sealed record PluginAgentFiles(string ProfileId, byte[] Profile, IReadOnlyDictionary<string, byte[]> Helpers);
