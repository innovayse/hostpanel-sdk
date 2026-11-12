namespace Hostpanel.Sdk.Packaging.Tests;

/// <summary>Tests of <see cref="PackagePath"/>: the one spelling of a path inside a package.</summary>
public sealed class PackagePathTests
{
    /// <summary>Backslashes become forward slashes; a path that has none is returned as it is.</summary>
    /// <param name="path">The path as written.</param>
    /// <param name="expected">The path with forward slashes.</param>
    [Theory]
    [InlineData("plugin.json", "plugin.json")]
    [InlineData("views/card.json", "views/card.json")]
    [InlineData("views\\card.json", "views/card.json")]
    [InlineData("a\\b/c\\d", "a/b/c/d")]
    [InlineData("", "")]
    public void Normalize_WritesForwardSlashes(string path, string expected)
    {
        PackagePath.Normalize(path).Should().Be(expected);
    }
}
