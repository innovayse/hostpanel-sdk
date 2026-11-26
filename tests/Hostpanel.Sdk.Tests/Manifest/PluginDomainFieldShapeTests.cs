using Hostpanel.Sdk.Plugins.Manifest;

namespace Hostpanel.Sdk.Tests.Manifest;

/// <summary>Tests of the shapes <see cref="PluginDomainFieldRules"/> accepts for a field key, a zone and a pattern.</summary>
public sealed class PluginDomainFieldShapeTests
{
    /// <summary>Letters, digits, dots, underscores, colons and hyphens after a letter or digit make a key.</summary>
    /// <param name="key">The key.</param>
    /// <param name="valid">Whether it is accepted.</param>
    [Theory]
    [InlineData("nexus", true)]
    [InlineData("x-nexus.category", true)]
    [InlineData("company_number:1", true)]
    [InlineData("A1", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("-a", false)]
    [InlineData(".a", false)]
    [InlineData("a b", false)]
    [InlineData("a/b", false)]
    [InlineData("kлюч", false)]
    public void IsValidKey_ChecksTheShape(string? key, bool valid)
    {
        PluginDomainFieldRules.IsValidKey(key).Should().Be(valid);
    }

    /// <summary>A key longer than the limit is refused and one of exactly the limit is accepted.</summary>
    [Fact]
    public void IsValidKey_LengthLimit_IsInclusive()
    {
        PluginDomainFieldRules.IsValidKey(new string('a', PluginDomainField.MaxKeyLength)).Should().BeTrue();
        PluginDomainFieldRules.IsValidKey(new string('a', PluginDomainField.MaxKeyLength + 1)).Should().BeFalse();
    }

    /// <summary>Lower case dot separated labels, punycode included, make a zone.</summary>
    /// <param name="zone">The zone.</param>
    /// <param name="valid">Whether it is accepted.</param>
    [Theory]
    [InlineData("com", true)]
    [InlineData("co.uk", true)]
    [InlineData("xn--p1ai", true)]
    [InlineData("a-b.c1", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData(".com", false)]
    [InlineData("com.", false)]
    [InlineData("co..uk", false)]
    [InlineData("COM", false)]
    [InlineData("-com", false)]
    [InlineData("com-", false)]
    [InlineData("пример", false)]
    [InlineData("a b", false)]
    public void IsValidZone_ChecksTheShape(string? zone, bool valid)
    {
        PluginDomainFieldRules.IsValidZone(zone).Should().Be(valid);
    }

    /// <summary>A label of 64 characters or a zone over 253 characters is refused.</summary>
    [Fact]
    public void IsValidZone_LengthLimits_Hold()
    {
        PluginDomainFieldRules.IsValidZone(new string('a', 63)).Should().BeTrue();
        PluginDomainFieldRules.IsValidZone(new string('a', 64)).Should().BeFalse();
        PluginDomainFieldRules.IsValidZone(string.Join('.', Enumerable.Repeat(new string('a', 62), 4))).Should().BeTrue();
        PluginDomainFieldRules.IsValidZone(string.Join('.', Enumerable.Repeat(new string('a', 63), 4))).Should().BeFalse();
    }

    /// <summary>A pattern builds an expression that must match the whole value; a bad one throws.</summary>
    [Fact]
    public void CreatePattern_Pattern_BuildsAWholeValueExpression()
    {
        var timeout = TimeSpan.FromSeconds(1);

        PluginDomainFieldRules.CreatePattern("[a-z]+", timeout).IsMatch("abc").Should().BeTrue();
        PluginDomainFieldRules.CreatePattern("[a-z]+", timeout).IsMatch("abc1").Should().BeFalse();
        FluentActions.Invoking(() => PluginDomainFieldRules.CreatePattern("(", timeout)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => PluginDomainFieldRules.CreatePattern("a)|(b", timeout)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => PluginDomainFieldRules.CreatePattern(new string('a', PluginDomainField.MaxPatternLength + 1), timeout))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => PluginDomainFieldRules.CreatePattern(null!, timeout)).Should().Throw<ArgumentNullException>();
    }
}
