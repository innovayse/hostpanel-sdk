namespace Hostpanel.Sdk.Packaging.Naming;

/// <summary>
/// The shape of one name that appears as a literal segment of an address: a page id, a literal part of a route and a slot
/// widget id. They are written into URLs and used as keys, so they share one rule and it is stated here once.
/// </summary>
public static class PluginSegment
{
    /// <summary>
    /// The accepted shape: a lowercase letter or digit, then lowercase letters, digits, hyphens and underscores.
    /// Anchored with <c>\A</c> and <c>\z</c>, never <c>^</c> and <c>$</c>, because <c>$</c> also matches before a
    /// trailing newline. The shape only: each user of it checks its own length.
    /// </summary>
    public const string Pattern = @"\A[a-z0-9][a-z0-9_-]*\z";
}
