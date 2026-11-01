using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.Lifecycle;

/// <summary>A plugin's answer to <see cref="Lifecycle.IPluginStatus.GetStatusAsync"/>.</summary>
/// <param name="Health">How well the plugin works.</param>
/// <param name="Detail">A short sentence for the administrator, or <see langword="null"/>; no secrets.</param>
public sealed record PluginStatusReport(PluginHealth Health, string? Detail = null);
