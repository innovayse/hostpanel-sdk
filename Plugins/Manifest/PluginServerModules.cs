using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>
/// The rules for <see cref="PluginManifest.ServerModules"/>, the list of server module ids a provisioning plugin serves.
/// A server module id is the short name an administrator picks for a server's control panel type; the host sends a
/// server to the plugin that lists its module. An id is 1 to <see cref="MaxIdLength"/> ASCII letters, digits, dashes,
/// dots or underscores, and ids are compared ignoring case.
/// </summary>
public static class PluginServerModules
{
    /// <summary>The most characters one id may have.</summary>
    public const int MaxIdLength = 50;

    /// <summary>
    /// Tells whether one id is well formed: not empty, at most <see cref="MaxIdLength"/> characters, and only ASCII
    /// letters, digits, dashes, dots and underscores. The check does not look at other ids.
    /// </summary>
    /// <param name="id">The id to check.</param>
    /// <returns><see langword="true"/> when the id may appear in a <c>serverModules</c> list.</returns>
    public static bool IsWellFormed(string? id)
    {
        return Check(id) is null;
    }

    /// <summary>
    /// Checks a whole <c>serverModules</c> list and reports every rule it breaks, in list order. A provisioning plugin that
    /// implements <see cref="Provisioning.IProvisioningPanelPlugin"/> or <see cref="VirtualServers.IVirtualServerPlugin"/> must list at least one id; the host calls this when
    /// it reads the manifest and refuses the plugin on any violation.
    /// </summary>
    /// <param name="serverModules">The list from the manifest; <see langword="null"/> is treated as empty.</param>
    /// <returns>The violations; empty when the list is acceptable.</returns>
    public static IReadOnlyList<PluginServerModuleViolation> Validate(IReadOnlyList<string>? serverModules)
    {
        if (serverModules is null || serverModules.Count == 0)
        {
            return [new PluginServerModuleViolation(PluginServerModuleRule.ListEmpty, null)];
        }

        var violations = new List<PluginServerModuleViolation>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < serverModules.Count; index++)
        {
            var id = serverModules[index];
            var rule = Check(id);
            if (rule is { } broken)
            {
                violations.Add(new PluginServerModuleViolation(broken, index));
                continue;
            }

            if (!seen.Add(id!))
            {
                violations.Add(new PluginServerModuleViolation(PluginServerModuleRule.IdDuplicate, index));
            }
        }

        return violations;
    }

    /// <summary>Checks one id against the rules that need no other id.</summary>
    /// <param name="id">The id to check.</param>
    /// <returns>The rule the id breaks, or <see langword="null"/> when it breaks none.</returns>
    private static PluginServerModuleRule? Check(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return PluginServerModuleRule.IdEmpty;
        }

        if (id.Length > MaxIdLength)
        {
            return PluginServerModuleRule.IdTooLong;
        }

        foreach (var character in id)
        {
            var allowed = character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '.' or '_';
            if (!allowed)
            {
                return PluginServerModuleRule.IdBadCharacters;
            }
        }

        return null;
    }
}
