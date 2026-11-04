using Hostpanel.Sdk.Packaging.Enums;
using Hostpanel.Sdk.Packaging.Naming;

namespace Hostpanel.Sdk.Packaging.Tests.Naming;

/// <summary>Tests of <see cref="PluginVersion"/>: strict parsing and semantic version precedence.</summary>
public sealed class PluginVersionTests
{
    /// <summary>Strict semantic versions parse, including a pre-release and build metadata.</summary>
    /// <param name="text">The version text.</param>
    [Theory]
    [InlineData("0.0.0")]
    [InlineData("1.2.3")]
    [InlineData("10.20.30")]
    [InlineData("1.0.0-alpha")]
    [InlineData("1.0.0-alpha.1")]
    [InlineData("1.0.0-0.3.7")]
    [InlineData("2.0.0-beta.1+7")]
    [InlineData("1.0.0+build.5")]
    public void TryParse_StrictVersion_Parses(string text)
    {
        PluginVersion.TryParse(text, out _).Should().BeTrue();
    }

    /// <summary>Anything lenient parsing would accept is refused, so two texts can never stand for one version.</summary>
    /// <param name="text">The text.</param>
    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("01.2.3")]
    [InlineData("1.02.3")]
    [InlineData(" 1.2.3")]
    [InlineData("1.2.3 ")]
    [InlineData("v1.2.3")]
    [InlineData("1.2.-3")]
    [InlineData("1.2.3-")]
    [InlineData("1.2.3-01")]
    [InlineData("1.2.3+")]
    [InlineData("1.2.3-al pha")]
    [InlineData("99999999999.0.0")]
    public void TryParse_NotAStrictVersion_IsRefused(string text)
    {
        PluginVersion.TryParse(text, out _).Should().BeFalse();
    }

    /// <summary>A missing text is not a version.</summary>
    [Fact]
    public void TryParse_Null_IsRefused()
    {
        PluginVersion.TryParse(null, out _).Should().BeFalse();
    }

    /// <summary>The order follows semantic version precedence, the example chain of the specification included.</summary>
    [Fact]
    public void CompareTo_FollowsSemanticVersionPrecedence()
    {
        string[] ascending =
        [
            "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta", "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1",
            "1.0.0", "1.0.1", "1.1.0", "2.0.0",
        ];

        for (var i = 0; i < ascending.Length - 1; i++)
        {
            Parse(ascending[i]).CompareTo(Parse(ascending[i + 1])).Should().BeNegative($"{ascending[i]} < {ascending[i + 1]}");
            Parse(ascending[i + 1]).CompareTo(Parse(ascending[i])).Should().BePositive();
        }
    }

    /// <summary>Build metadata does not affect precedence.</summary>
    [Fact]
    public void CompareTo_BuildMetadata_IsIgnored()
    {
        Parse("1.0.0+a").CompareTo(Parse("1.0.0+b")).Should().Be(0);
    }

    /// <summary>The change between an installed and an uploaded version is told from their order.</summary>
    /// <param name="installed">The installed version.</param>
    /// <param name="uploaded">The uploaded version.</param>
    /// <param name="expected">The change.</param>
    [Theory]
    [InlineData("1.0.0", "1.0.1", PluginVersionChange.Newer)]
    [InlineData("1.0.0", "1.0.0", PluginVersionChange.Same)]
    [InlineData("1.0.1", "1.0.0", PluginVersionChange.Older)]
    [InlineData("1.0.0", "1.0.0-rc.1", PluginVersionChange.Older)]
    [InlineData("1.0.0", "garbage", PluginVersionChange.Incomparable)]
    [InlineData("garbage", "1.0.0", PluginVersionChange.Incomparable)]
    public void Compare_TellsTheChange(string installed, string uploaded, PluginVersionChange expected)
    {
        PluginVersion.Compare(installed, uploaded).Should().Be(expected);
    }

    /// <summary>Parses a version that the test knows is valid.</summary>
    /// <param name="text">The version text.</param>
    /// <returns>The version.</returns>
    private static PluginVersion Parse(string text)
    {
        PluginVersion.TryParse(text, out var version).Should().BeTrue();
        return version;
    }
}
