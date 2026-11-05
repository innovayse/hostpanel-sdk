namespace Hostpanel.Sdk.Packaging;

/// <summary>
/// The one limit every regular expression in this package runs under. The patterns are linear on the sizes the checks
/// allow, so the limit never fires on honest input; it is the bound for a crafted one, and a pattern that hits it makes
/// the checked value invalid instead of keeping a thread busy.
/// </summary>
internal static class RegexLimits
{
    /// <summary>How long one regular expression may run on one text, in milliseconds.</summary>
    internal const int TimeoutMilliseconds = 1000;
}
