using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;

namespace Hostpanel.Sdk.Tests.Manifest;

/// <summary>Tests of <see cref="PluginDomainFieldRules.Validate"/>: whether the values given for the fields of a domain are acceptable.</summary>
public sealed class PluginDomainFieldValidateTests
{
    /// <summary>Builds a field of one kind.</summary>
    /// <param name="type">The input kind.</param>
    /// <param name="required">Whether the field is required.</param>
    /// <returns>The field, keyed <c>f</c>.</returns>
    private static PluginDomainField Field(PluginDomainFieldType type, bool required = false)
    {
        return new PluginDomainField { Key = "f", Label = "f", Type = type, Required = required };
    }

    /// <summary>Validates one value of one field.</summary>
    /// <param name="field">The field.</param>
    /// <param name="value">The value, or <see langword="null"/> to send none.</param>
    /// <returns>The reasons reported for key <c>f</c>.</returns>
    private static PluginDomainFieldViolationReason[] Reasons(PluginDomainField field, string? value)
    {
        var attributes = value is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["f"] = value };
        return PluginDomainFieldRules.Validate([field], attributes).Select(violation => violation.Reason).ToArray();
    }

    /// <summary>Acceptable values report nothing.</summary>
    [Fact]
    public void Validate_AcceptableValues_ReturnsNothing()
    {
        PluginDomainFieldRules.Validate([], new Dictionary<string, string>()).Should().BeEmpty();
        Reasons(Field(PluginDomainFieldType.Text, required: true), "abc").Should().BeEmpty();
        Reasons(Field(PluginDomainFieldType.Text), null).Should().BeEmpty();
    }

    /// <summary>A required field with no value, an empty one or white space is Required; an optional one is fine.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RequiredWithoutValue_ReportsRequired(string? value)
    {
        Reasons(Field(PluginDomainFieldType.Text, required: true), value).Should().Equal(PluginDomainFieldViolationReason.Required);
        Reasons(Field(PluginDomainFieldType.Text), value).Should().BeEmpty();
        Reasons(Field(PluginDomainFieldType.Date), value).Should().BeEmpty();
    }

    /// <summary>An attribute that is no declared field is Unknown, listed after the declared ones in key order.</summary>
    [Fact]
    public void Validate_UndeclaredKeys_ReportUnknownInKeyOrder()
    {
        var violations = PluginDomainFieldRules.Validate(
            [Field(PluginDomainFieldType.Text, required: true)],
            new Dictionary<string, string> { ["zeta"] = "1", ["alpha"] = "2", ["F"] = "3" });

        violations.Should().Equal(
            new PluginDomainFieldViolation("f", PluginDomainFieldViolationReason.Required),
            new PluginDomainFieldViolation("F", PluginDomainFieldViolationReason.Unknown),
            new PluginDomainFieldViolation("alpha", PluginDomainFieldViolationReason.Unknown),
            new PluginDomainFieldViolation("zeta", PluginDomainFieldViolationReason.Unknown));
    }

    /// <summary>A select value must be one of the options exactly.</summary>
    [Theory]
    [InlineData("a", false)]
    [InlineData("b", false)]
    [InlineData("A", true)]
    [InlineData("c", true)]
    public void Validate_Select_ChecksOptions(string value, bool refused)
    {
        var field = new PluginDomainField
        {
            Key = "f",
            Label = "f",
            Type = PluginDomainFieldType.Select,
            Options = [new PluginSettingOption { Value = "a", Label = "A" }, new PluginSettingOption { Value = "b", Label = "B" }],
        };

        Reasons(field, value).Should().Equal(refused ? [PluginDomainFieldViolationReason.Option] : []);
    }

    /// <summary>A date is yyyy-MM-dd and a real calendar day.</summary>
    [Theory]
    [InlineData("2026-11-26", false)]
    [InlineData("2024-02-29", false)]
    [InlineData("26.11.2026", true)]
    [InlineData("2026-1-5", true)]
    [InlineData("2026-02-30", true)]
    [InlineData("2026-11-26T10:00:00", true)]
    [InlineData("tomorrow", true)]
    public void Validate_Date_ChecksFormat(string value, bool refused)
    {
        Reasons(Field(PluginDomainFieldType.Date), value).Should().Equal(refused ? [PluginDomainFieldViolationReason.Format] : []);
    }

    /// <summary>A number has an optional sign and decimal point and nothing else.</summary>
    [Theory]
    [InlineData("42", false)]
    [InlineData("-3.5", false)]
    [InlineData("+7", false)]
    [InlineData("1,000", true)]
    [InlineData("1e3", true)]
    [InlineData("12 ", true)]
    [InlineData("abc", true)]
    public void Validate_Number_ChecksFormat(string value, bool refused)
    {
        Reasons(Field(PluginDomainFieldType.Number), value).Should().Equal(refused ? [PluginDomainFieldViolationReason.Format] : []);
    }

    /// <summary>A web address is absolute and uses http or https.</summary>
    [Theory]
    [InlineData("https://example.com/a?b=1", false)]
    [InlineData("http://example.com", false)]
    [InlineData("example.com", true)]
    [InlineData("/relative", true)]
    [InlineData("ftp://example.com", true)]
    [InlineData("javascript:alert(1)", true)]
    public void Validate_Url_ChecksFormat(string value, bool refused)
    {
        Reasons(Field(PluginDomainFieldType.Url), value).Should().Equal(refused ? [PluginDomainFieldViolationReason.Format] : []);
    }

    /// <summary>A value outside the length bounds is a Length violation; the bounds are inclusive.</summary>
    [Theory]
    [InlineData("ab", true)]
    [InlineData("abc", false)]
    [InlineData("abcde", false)]
    [InlineData("abcdef", true)]
    public void Validate_Length_ChecksBounds(string value, bool refused)
    {
        var field = new PluginDomainField { Key = "f", Label = "f", Type = PluginDomainFieldType.Text, MinLength = 3, MaxLength = 5 };

        Reasons(field, value).Should().Equal(refused ? [PluginDomainFieldViolationReason.Length] : []);
    }

    /// <summary>The pattern has to match the whole value, not a part of it.</summary>
    [Theory]
    [InlineData("12345", false)]
    [InlineData("1234", true)]
    [InlineData("a12345", true)]
    [InlineData("12345a", true)]
    [InlineData("12345\n", true)]
    public void Validate_Pattern_MatchesTheWholeValue(string value, bool refused)
    {
        var field = new PluginDomainField { Key = "f", Label = "f", Type = PluginDomainFieldType.Text, Pattern = "[0-9]{5}" };

        Reasons(field, value).Should().Equal(refused ? [PluginDomainFieldViolationReason.Pattern] : []);
    }

    /// <summary>An alternation is bound to the whole value too, not to its first or last branch.</summary>
    [Fact]
    public void Validate_PatternWithAlternation_IsAnchoredAsAWhole()
    {
        var field = new PluginDomainField { Key = "f", Label = "f", Type = PluginDomainFieldType.Text, Pattern = "ab|cd" };

        Reasons(field, "ab").Should().BeEmpty();
        Reasons(field, "abcd").Should().Equal(PluginDomainFieldViolationReason.Pattern);
    }

    /// <summary>A pattern that is not a valid expression refuses every value instead of letting one through.</summary>
    [Theory]
    [InlineData("(")]
    [InlineData("a)|(b")]
    public void Validate_InvalidPattern_RefusesTheValue(string pattern)
    {
        var field = new PluginDomainField { Key = "f", Label = "f", Type = PluginDomainFieldType.Text, Pattern = pattern };

        Reasons(field, "a").Should().Equal(PluginDomainFieldViolationReason.Pattern);
    }

    /// <summary>A pattern that runs past the time limit refuses the value.</summary>
    [Fact]
    public void Validate_PatternRunningTooLong_RefusesTheValue()
    {
        var field = new PluginDomainField { Key = "f", Label = "f", Type = PluginDomainFieldType.Text, Pattern = "(a+)+$" };

        Reasons(field, new string('a', 60) + "!").Should().Equal(PluginDomainFieldViolationReason.Pattern);
    }

    /// <summary>The first failing check is the one reported: the format before the length, the length before the pattern.</summary>
    [Fact]
    public void Validate_SeveralChecksFail_ReportsTheFirst()
    {
        var field = new PluginDomainField { Key = "f", Label = "f", Type = PluginDomainFieldType.Number, MaxLength = 2, Pattern = "[0-9]+" };

        Reasons(field, "abc").Should().Equal(PluginDomainFieldViolationReason.Format);
        Reasons(field, "123").Should().Equal(PluginDomainFieldViolationReason.Length);
        Reasons(field, "-1").Should().Equal(PluginDomainFieldViolationReason.Pattern);
    }

    /// <summary>The arguments must not be null.</summary>
    [Fact]
    public void Validate_NullArguments_Throw()
    {
        FluentActions.Invoking(() => PluginDomainFieldRules.Validate(null!, new Dictionary<string, string>()))
            .Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => PluginDomainFieldRules.Validate([], null!)).Should().Throw<ArgumentNullException>();
    }
}
