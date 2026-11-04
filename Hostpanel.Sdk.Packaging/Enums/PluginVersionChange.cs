namespace Hostpanel.Sdk.Packaging.Enums;

/// <summary>How an uploaded plugin version relates to the installed one. Values start at 1; 0 is not a change.</summary>
public enum PluginVersionChange
{
    /// <summary>The uploaded version has higher precedence than the installed one.</summary>
    Newer = 1,

    /// <summary>The two versions have equal precedence.</summary>
    Same = 2,

    /// <summary>The uploaded version has lower precedence than the installed one.</summary>
    Older = 3,

    /// <summary>At least one of the two texts is not a strict version, so no order can be proven.</summary>
    Incomparable = 4,
}
