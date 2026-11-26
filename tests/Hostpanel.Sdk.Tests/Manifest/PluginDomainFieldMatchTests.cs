using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;

namespace Hostpanel.Sdk.Tests.Manifest;

/// <summary>Tests of <see cref="PluginDomainFieldRules.Match"/>: which fields a manifest asks for one domain.</summary>
public sealed class PluginDomainFieldMatchTests
{
    /// <summary>Builds a one field list whose key is the only thing a test looks at.</summary>
    /// <param name="key">The field key.</param>
    /// <returns>The fields.</returns>
    private static PluginDomainField[] Fields(string key)
    {
        return [new PluginDomainField { Key = key, Label = key, Type = PluginDomainFieldType.Text }];
    }

    /// <summary>Builds a registrar manifest with the given sets.</summary>
    /// <param name="sets">The sets of domain fields.</param>
    /// <returns>The manifest.</returns>
    private static PluginManifest Manifest(params PluginDomainFieldSet[] sets)
    {
        return new PluginManifest
        {
            Id = "demo",
            Name = "Demo",
            Version = "1.0.0",
            Author = "A",
            Description = "D",
            Type = PluginType.Registrar,
            Category = "Domains",
            EntryPoint = "Demo.Plugin",
            SdkVersion = "1.0",
            ManifestVersion = 2,
            DomainFields = sets,
        };
    }

    /// <summary>Reads the keys of the fields answered for a domain, for a short assertion.</summary>
    /// <param name="manifest">The manifest.</param>
    /// <param name="domain">The domain name.</param>
    /// <param name="holder">The registrant kind.</param>
    /// <param name="operation">The operation.</param>
    /// <returns>The keys in order.</returns>
    private static string[] Keys(PluginManifest manifest, string domain, PluginDomainHolder holder = PluginDomainHolder.Individual, PluginDomainOperations operation = PluginDomainOperations.Register)
    {
        return PluginDomainFieldRules.Match(manifest, domain, holder, operation).Select(field => field.Key).ToArray();
    }

    /// <summary>A manifest without domain fields asks for none.</summary>
    [Fact]
    public void Match_NoSets_ReturnsNothing()
    {
        Keys(Manifest(), "example.com").Should().BeEmpty();
    }

    /// <summary>A domain below the zone gets its fields; case and a trailing dot do not matter.</summary>
    [Theory]
    [InlineData("example.us")]
    [InlineData("Example.US")]
    [InlineData("example.us.")]
    [InlineData("www.example.us")]
    public void Match_DomainBelowZone_ReturnsFields(string domain)
    {
        var manifest = Manifest(new PluginDomainFieldSet { Zones = ["us"], Fields = Fields("nexus") });

        Keys(manifest, domain).Should().Equal("nexus");
    }

    /// <summary>A name that merely ends with the same letters, the zone itself or another zone gets nothing.</summary>
    [Theory]
    [InlineData("example.bus")]
    [InlineData("us")]
    [InlineData("example.com")]
    [InlineData("")]
    [InlineData("  ")]
    public void Match_DomainOutsideZone_ReturnsNothing(string domain)
    {
        var manifest = Manifest(new PluginDomainFieldSet { Zones = ["us"], Fields = Fields("nexus") });

        Keys(manifest, domain).Should().BeEmpty();
    }

    /// <summary>The set with the longest matching zone wins, wherever it is declared.</summary>
    [Fact]
    public void Match_SeveralZonesFit_LongestSuffixWins()
    {
        var manifest = Manifest(
            new PluginDomainFieldSet { Zones = ["co.uk"], Fields = Fields("long") },
            new PluginDomainFieldSet { Zones = ["uk"], Fields = Fields("short") });

        Keys(manifest, "example.co.uk").Should().Equal("long");
        Keys(manifest, "example.org.uk").Should().Equal("short");
    }

    /// <summary>A longer zone with no fields overrides the fields of a shorter one.</summary>
    [Fact]
    public void Match_LongerSetWithoutFields_ReturnsNothing()
    {
        var manifest = Manifest(
            new PluginDomainFieldSet { Zones = ["uk"], Fields = Fields("short") },
            new PluginDomainFieldSet { Zones = ["co.uk"] });

        Keys(manifest, "example.co.uk").Should().BeEmpty();
    }

    /// <summary>A unicode domain name is read in its punycode form and a punycode zone matches it.</summary>
    [Theory]
    [InlineData("пример.рф")]
    [InlineData("xn--e1afmkfd.xn--p1ai")]
    [InlineData("XN--E1AFMKFD.XN--P1AI")]
    public void Match_PunycodeZone_MatchesUnicodeAndPunycodeDomains(string domain)
    {
        var manifest = Manifest(new PluginDomainFieldSet { Zones = ["xn--p1ai"], Fields = Fields("inn") });

        Keys(manifest, domain).Should().Equal("inn");
    }

    /// <summary>A name that cannot be written in punycode matches nothing.</summary>
    [Fact]
    public void Match_UnconvertibleName_ReturnsNothing()
    {
        var manifest = Manifest(new PluginDomainFieldSet { Zones = ["us"], Fields = Fields("nexus") });

        Keys(manifest, "exa..mple.us").Should().BeEmpty();
    }

    /// <summary>A set for one holder applies to that holder only; a set for any holder applies to both.</summary>
    [Fact]
    public void Match_Holder_FiltersSets()
    {
        var manifest = Manifest(
            new PluginDomainFieldSet { Zones = ["de"], Holder = PluginDomainHolder.Organization, Fields = Fields("company") },
            new PluginDomainFieldSet { Zones = ["fr"], Fields = Fields("any") });

        Keys(manifest, "example.de", PluginDomainHolder.Organization).Should().Equal("company");
        Keys(manifest, "example.de", PluginDomainHolder.Individual).Should().BeEmpty();
        Keys(manifest, "example.de", PluginDomainHolder.Any).Should().BeEmpty();
        Keys(manifest, "example.fr", PluginDomainHolder.Individual).Should().Equal("any");
        Keys(manifest, "example.fr", PluginDomainHolder.Organization).Should().Equal("any");
    }

    /// <summary>One zone may carry a set per holder, and each holder gets its own.</summary>
    [Fact]
    public void Match_SetPerHolder_EachHolderGetsItsOwn()
    {
        var manifest = Manifest(
            new PluginDomainFieldSet { Zones = ["it"], Holder = PluginDomainHolder.Individual, Fields = Fields("taxCode") },
            new PluginDomainFieldSet { Zones = ["it"], Holder = PluginDomainHolder.Organization, Fields = Fields("vat") });

        Keys(manifest, "example.it", PluginDomainHolder.Individual).Should().Equal("taxCode");
        Keys(manifest, "example.it", PluginDomainHolder.Organization).Should().Equal("vat");
    }

    /// <summary>A set applies to the operations it names, and to all of them when it names none.</summary>
    [Fact]
    public void Match_Operation_FiltersSets()
    {
        var manifest = Manifest(
            new PluginDomainFieldSet { Zones = ["de"], Operations = PluginDomainOperations.Register | PluginDomainOperations.Transfer, Fields = Fields("two") },
            new PluginDomainFieldSet { Zones = ["fr"], Fields = Fields("all") });

        Keys(manifest, "example.de", operation: PluginDomainOperations.Register).Should().Equal("two");
        Keys(manifest, "example.de", operation: PluginDomainOperations.Transfer).Should().Equal("two");
        Keys(manifest, "example.de", operation: PluginDomainOperations.ContactChange).Should().BeEmpty();
        Keys(manifest, "example.fr", operation: PluginDomainOperations.ContactChange).Should().Equal("all");
    }

    /// <summary>An operation set must name one operation, and the arguments must not be null.</summary>
    [Fact]
    public void Match_BadArguments_Throw()
    {
        var manifest = Manifest();

        FluentActions.Invoking(() => PluginDomainFieldRules.Match(manifest, "example.com", PluginDomainHolder.Any, PluginDomainOperations.None))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => PluginDomainFieldRules.Match(null!, "example.com", PluginDomainHolder.Any, PluginDomainOperations.Register))
            .Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => PluginDomainFieldRules.Match(manifest, null!, PluginDomainHolder.Any, PluginDomainOperations.Register))
            .Should().Throw<ArgumentNullException>();
    }

    /// <summary>A manifest that never mentions domain fields reads as an empty list, whether the member is missing or null.</summary>
    [Fact]
    public void DomainFields_NullOrMissing_ReadsAsEmpty()
    {
        Manifest().DomainFields.Should().BeEmpty();
        new PluginManifest
        {
            Id = "demo",
            Name = "Demo",
            Version = "1.0.0",
            Author = "A",
            Description = "D",
            Type = PluginType.Registrar,
            Category = "Domains",
            EntryPoint = "Demo.Plugin",
            SdkVersion = "1.0",
            DomainFields = null!,
        }.DomainFields.Should().BeEmpty();
        new PluginDomainFieldSet { Zones = null!, Fields = null! }.Zones.Should().BeEmpty();
        new PluginDomainField { Key = "k", Label = "l", Type = PluginDomainFieldType.Text, Options = null! }.Options.Should().BeEmpty();
        new PluginDomainFieldSet().Holder.Should().Be(PluginDomainHolder.Any);
        new PluginDomainFieldSet().Operations.Should().Be(PluginDomainOperations.All);
    }
}
