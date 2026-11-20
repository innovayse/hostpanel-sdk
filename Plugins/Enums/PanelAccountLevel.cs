namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>The kind of account a hosting panel holds: a plain hosting account or a reseller that owns accounts of its own. The values start at 1 and 0 is not valid.</summary>
public enum PanelAccountLevel
{
    /// <summary>An ordinary hosting account, the default of every panel.</summary>
    User = 1,

    /// <summary>A reseller account, which can create and manage accounts of its own.</summary>
    Reseller = 2,
}
