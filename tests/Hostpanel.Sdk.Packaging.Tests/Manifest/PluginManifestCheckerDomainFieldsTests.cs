using Hostpanel.Sdk.Packaging.Enums;
using Hostpanel.Sdk.Packaging.Manifest;
using Hostpanel.Sdk.Packaging.Tests.Support;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;

namespace Hostpanel.Sdk.Packaging.Tests.Manifest;

/// <summary>Tests of the <c>domainFields</c> rules of <see cref="PluginManifestChecker"/>.</summary>
public sealed class PluginManifestCheckerDomainFieldsTests
{
    /// <summary>The host's decisions the tests use: the default manifest versions and no client permission names.</summary>
    private static readonly PluginManifestOptions Options = new() { ClientPermissionNames = new HashSet<string>(StringComparer.Ordinal) };

    /// <summary>Checks a registrar manifest made of the required members plus the given <c>domainFields</c> JSON array.</summary>
    /// <param name="domainFields">The JSON array written in <c>domainFields</c>.</param>
    /// <param name="extraMembers">More JSON members, without braces; empty for none.</param>
    /// <param name="type">The plugin type name.</param>
    /// <returns>The violation, or <see langword="null"/>.</returns>
    private static PluginManifestViolation? Check(string domainFields, string extraMembers = "", string type = "Registrar")
    {
        var members = "\"manifestVersion\":2,\"domainFields\":" + domainFields + (extraMembers.Length == 0 ? string.Empty : "," + extraMembers);
        return PluginManifestChecker.Validate(ManifestJson.Read(members, type), Options);
    }

    /// <summary>Asserts that the <c>domainFields</c> break exactly the given rule.</summary>
    /// <param name="domainFields">The JSON array written in <c>domainFields</c>.</param>
    /// <param name="rule">The rule expected.</param>
    private static void AssertBreaks(string domainFields, PluginManifestRule rule)
    {
        Check(domainFields)!.Rule.Should().Be(rule);
    }

    /// <summary>Wraps one field member list into a set of the zone <c>us</c>.</summary>
    /// <param name="field">The members of one field, without braces.</param>
    /// <returns>The JSON array of one set with one field.</returns>
    private static string OneField(string field)
    {
        return "[{\"zones\":[\"us\"],\"fields\":[{" + field + "}]}]";
    }

    /// <summary>A registrar manifest that uses every member of the block correctly passes and reads back as written.</summary>
    [Fact]
    public void Validate_FullBlock_PassesAndReadsBack()
    {
        const string block = """
            [
              {"zones":["us","xn--p1ai"],"holder":"organization","operations":"register, transfer",
               "fields":[
                 {"key":"nexus","label":"domain.nexus","description":"domain.nexus.help","placeholder":"domain.nexus.hint","type":"select","required":true,
                  "options":[{"value":"c11","label":"domain.nexus.c11"},{"value":"c21","label":"Citizen"}]},
                 {"key":"taxId","label":"domain.taxId","type":"text","pattern":"[0-9]{9}","minLength":9,"maxLength":9},
                 {"key":"founded","label":"Founded","type":"date"}
               ]},
              {"zones":["us"],"holder":"individual","fields":[]}
            ]
            """;
        const string locales = "\"locales\":{\"en\":{\"domain.nexus\":\"Nexus\",\"domain.nexus.help\":\"Why\",\"domain.nexus.hint\":\"Pick\",\"domain.nexus.c11\":\"Citizen\",\"domain.taxId\":\"Tax id\"}}";

        var manifest = ManifestJson.Read("\"manifestVersion\":2,\"domainFields\":" + block + "," + locales, "Registrar");

        PluginManifestChecker.Validate(manifest, Options).Should().BeNull();
        var set = manifest.DomainFields[0];
        set.Zones.Should().Equal("us", "xn--p1ai");
        set.Holder.Should().Be(PluginDomainHolder.Organization);
        set.Operations.Should().Be(PluginDomainOperations.Register | PluginDomainOperations.Transfer);
        set.Fields[0].Type.Should().Be(PluginDomainFieldType.Select);
        set.Fields[0].Options.Should().HaveCount(2);
        set.Fields[1].MinLength.Should().Be(9);
        manifest.DomainFields[1].Operations.Should().Be(PluginDomainOperations.All);
    }

    /// <summary>A manifest without the block meets the rules, as one written before 3.17.0 does.</summary>
    [Fact]
    public void Validate_NoBlock_Passes()
    {
        PluginManifestChecker.Validate(ManifestJson.Read(string.Empty, "Registrar"), Options).Should().BeNull();
    }

    /// <summary>Any plugin type but a registrar plugin is refused a block, whatever it holds.</summary>
    [Theory]
    [InlineData("Provisioning")]
    [InlineData("Payment")]
    [InlineData("App")]
    public void Validate_BlockOnOtherType_BreaksDomainFieldsNotRegistrar(string type)
    {
        Check(OneField("\"key\":\"a\",\"label\":\"A\",\"type\":\"text\""), type: type)!.Rule.Should().Be(PluginManifestRule.DomainFieldsNotRegistrar);
    }

    /// <summary>A key that is empty, starts with a symbol, holds a space or is too long breaks the key rule.</summary>
    /// <param name="key">The key written in the field.</param>
    [Theory]
    [InlineData("")]
    [InlineData("-a")]
    [InlineData("a b")]
    [InlineData("a/b")]
    public void Validate_BadKey_BreaksDomainFieldKeyInvalid(string key)
    {
        AssertBreaks(OneField($"\"key\":\"{key}\",\"label\":\"A\",\"type\":\"text\""), PluginManifestRule.DomainFieldKeyInvalid);
    }

    /// <summary>A key longer than the limit breaks the key rule.</summary>
    [Fact]
    public void Validate_KeyTooLong_BreaksDomainFieldKeyInvalid()
    {
        AssertBreaks(
            OneField($"\"key\":\"{new string('a', PluginDomainField.MaxKeyLength + 1)}\",\"label\":\"A\",\"type\":\"text\""),
            PluginManifestRule.DomainFieldKeyInvalid);
    }

    /// <summary>The same key twice in one set breaks the key rule; the same key in two sets is fine.</summary>
    [Fact]
    public void Validate_DuplicateKey_BreaksOnlyWithinASet()
    {
        const string field = "{\"key\":\"a\",\"label\":\"A\",\"type\":\"text\"}";

        AssertBreaks($"[{{\"zones\":[\"us\"],\"fields\":[{field},{field}]}}]", PluginManifestRule.DomainFieldKeyInvalid);
        Check($"[{{\"zones\":[\"us\"],\"fields\":[{field}]}},{{\"zones\":[\"de\"],\"fields\":[{field}]}}]").Should().BeNull();
    }

    /// <summary>A set with no zones, or a zone that is upper case, has a leading dot, an empty label or is not ASCII breaks the zone rule.</summary>
    /// <param name="zones">The JSON array written in <c>zones</c>.</param>
    [Theory]
    [InlineData("[]")]
    [InlineData("[\"US\"]")]
    [InlineData("[\".us\"]")]
    [InlineData("[\"co..uk\"]")]
    [InlineData("[\"us\",\"пример\"]")]
    [InlineData("[\"\"]")]
    public void Validate_BadZone_BreaksDomainZoneInvalid(string zones)
    {
        AssertBreaks("[{\"zones\":" + zones + "}]", PluginManifestRule.DomainZoneInvalid);
    }

    /// <summary>A set that names no zones at all (the member is missing) breaks the zone rule too.</summary>
    [Fact]
    public void Validate_MissingZones_BreaksDomainZoneInvalid()
    {
        AssertBreaks("[{\"fields\":[]}]", PluginManifestRule.DomainZoneInvalid);
    }

    /// <summary>A zone named twice, within one set or across two sets that overlap in holder and operation, breaks the duplicate rule.</summary>
    [Theory]
    [InlineData("[{\"zones\":[\"us\"]},{\"zones\":[\"de\",\"us\"]}]")]
    [InlineData("[{\"zones\":[\"us\"],\"holder\":\"individual\"},{\"zones\":[\"us\"]}]")]
    [InlineData("[{\"zones\":[\"us\"],\"operations\":\"register, transfer\"},{\"zones\":[\"us\"],\"operations\":\"transfer\"}]")]
    public void Validate_OverlappingSets_BreakDomainZoneDuplicate(string block)
    {
        AssertBreaks(block, PluginManifestRule.DomainZoneDuplicate);
    }

    /// <summary>The same zone for different holders, or for operations that do not overlap, is fine.</summary>
    [Theory]
    [InlineData("[{\"zones\":[\"us\"],\"holder\":\"individual\"},{\"zones\":[\"us\"],\"holder\":\"organization\"}]")]
    [InlineData("[{\"zones\":[\"us\"],\"operations\":\"register\"},{\"zones\":[\"us\"],\"operations\":\"transfer, contactChange\"}]")]
    [InlineData("[{\"zones\":[\"uk\"]},{\"zones\":[\"co.uk\"]}]")]
    public void Validate_SetsThatDoNotOverlap_Pass(string block)
    {
        Check(block).Should().BeNull();
    }

    /// <summary>An operation list that names nothing, or a bit that is none of the operations, breaks the operations rule.</summary>
    /// <param name="operations">The JSON value written in <c>operations</c>.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("8")]
    [InlineData("15")]
    public void Validate_BadOperations_BreakDomainOperationsInvalid(string operations)
    {
        AssertBreaks("[{\"zones\":[\"us\"],\"operations\":" + operations + "}]", PluginManifestRule.DomainOperationsInvalid);
    }

    /// <summary>Options on a field that is not a select, and a select with none, a repeated value or an empty one, break the options rule.</summary>
    /// <param name="field">The members of the field, without braces.</param>
    [Theory]
    [InlineData("\"key\":\"a\",\"label\":\"A\",\"type\":\"text\",\"options\":[{\"value\":\"x\",\"label\":\"X\"}]")]
    [InlineData("\"key\":\"a\",\"label\":\"A\",\"type\":\"select\"")]
    [InlineData("\"key\":\"a\",\"label\":\"A\",\"type\":\"select\",\"options\":[]")]
    [InlineData("\"key\":\"a\",\"label\":\"A\",\"type\":\"select\",\"options\":[{\"value\":\"x\",\"label\":\"X\"},{\"value\":\"x\",\"label\":\"Y\"}]")]
    [InlineData("\"key\":\"a\",\"label\":\"A\",\"type\":\"select\",\"options\":[{\"value\":\"\",\"label\":\"X\"}]")]
    public void Validate_BadOptions_BreakDomainFieldOptionsInvalid(string field)
    {
        AssertBreaks(OneField(field), PluginManifestRule.DomainFieldOptionsInvalid);
    }

    /// <summary>A pattern that is not a regular expression, an unbalanced one or one over the length limit breaks the pattern rule.</summary>
    /// <param name="pattern">The pattern, as JSON text content.</param>
    [Theory]
    [InlineData("(")]
    [InlineData("a)|(b")]
    [InlineData("[")]
    public void Validate_BadPattern_BreaksDomainFieldPatternInvalid(string pattern)
    {
        AssertBreaks(OneField($"\"key\":\"a\",\"label\":\"A\",\"type\":\"text\",\"pattern\":\"{pattern}\""), PluginManifestRule.DomainFieldPatternInvalid);
    }

    /// <summary>A pattern one character over the limit breaks the pattern rule and one at the limit passes.</summary>
    [Fact]
    public void Validate_PatternLength_LimitIsInclusive()
    {
        Check(OneField($"\"key\":\"a\",\"label\":\"A\",\"type\":\"text\",\"pattern\":\"{new string('a', PluginDomainField.MaxPatternLength)}\"")).Should().BeNull();
        AssertBreaks(
            OneField($"\"key\":\"a\",\"label\":\"A\",\"type\":\"text\",\"pattern\":\"{new string('a', PluginDomainField.MaxPatternLength + 1)}\""),
            PluginManifestRule.DomainFieldPatternInvalid);
    }

    /// <summary>A negative length bound, or a minimum above the maximum, breaks the length rule; equal bounds pass.</summary>
    /// <param name="bounds">The length members, without braces.</param>
    /// <param name="broken">Whether the bounds break the rule.</param>
    [Theory]
    [InlineData("\"minLength\":-1", true)]
    [InlineData("\"maxLength\":-1", true)]
    [InlineData("\"minLength\":5,\"maxLength\":4", true)]
    [InlineData("\"minLength\":4,\"maxLength\":4", false)]
    [InlineData("\"minLength\":0", false)]
    public void Validate_LengthBounds_AreChecked(string bounds, bool broken)
    {
        var violation = Check(OneField("\"key\":\"a\",\"label\":\"A\",\"type\":\"text\"," + bounds));

        if (broken)
        {
            violation!.Rule.Should().Be(PluginManifestRule.DomainFieldLengthInvalid);
        }
        else
        {
            violation.Should().BeNull();
        }
    }

    /// <summary>A label, description, placeholder or option label that is a text key must be defined in English.</summary>
    /// <param name="field">The members of the field, without braces.</param>
    [Theory]
    [InlineData("\"key\":\"a\",\"label\":\"domain.missing\",\"type\":\"text\"")]
    [InlineData("\"key\":\"a\",\"label\":\"A\",\"type\":\"text\",\"description\":\"domain.help\"")]
    [InlineData("\"key\":\"a\",\"label\":\"A\",\"type\":\"text\",\"placeholder\":\"domain.hint\"")]
    [InlineData("\"key\":\"a\",\"label\":\"A\",\"type\":\"select\",\"options\":[{\"value\":\"x\",\"label\":\"domain.option\"}]")]
    public void Validate_UndefinedTextKey_BreaksLabelKeyMissing(string field)
    {
        // The German text defines the help and hint keys, which makes them keys; none of them is defined in English.
        const string locales = "\"locales\":{\"en\":{},\"de\":{\"domain.help\":\"Hilfe\",\"domain.hint\":\"Tipp\"}}";

        Check(OneField(field), locales)!.Rule.Should().Be(PluginManifestRule.LabelKeyMissing);
    }

    /// <summary>Plain text in a description or placeholder is shown as written and needs no entry.</summary>
    [Fact]
    public void Validate_PlainHelpText_Passes()
    {
        Check(OneField("\"key\":\"a\",\"label\":\"A\",\"type\":\"text\",\"description\":\"e.g. 12.345\",\"placeholder\":\"name.example\"")).Should().BeNull();
    }
}
