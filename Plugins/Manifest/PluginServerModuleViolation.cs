using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>One way a <c>serverModules</c> list breaks the rules of <see cref="PluginServerModules"/>.</summary>
/// <param name="Rule">The rule that is broken.</param>
/// <param name="Index">The zero based position of the offending entry, or <see langword="null"/> when the rule concerns the whole list.</param>
public sealed record PluginServerModuleViolation(PluginServerModuleRule Rule, int? Index);
