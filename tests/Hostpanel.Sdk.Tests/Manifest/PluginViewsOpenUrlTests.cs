using Hostpanel.Sdk.Plugins.Ui;

namespace Hostpanel.Sdk.Tests.Manifest;

/// <summary>Tests of <see cref="PluginViews.OpenUrlFromProperty"/> and <see cref="PluginViews.IsOpenUrlFieldName"/>.</summary>
public sealed class PluginViewsOpenUrlTests
{
    /// <summary>The JSON member is <c>openUrlFrom</c>, camelCase.</summary>
    [Fact]
    public void OpenUrlFromProperty_IsCamelCaseName()
    {
        PluginViews.OpenUrlFromProperty.Should().Be("openUrlFrom");
    }

    /// <summary>The new member is not a label or a route member, so the host neither translates nor resolves it.</summary>
    [Fact]
    public void OpenUrlFromProperty_IsNeitherLabelNorRoute()
    {
        PluginViews.LabelProperties.Should().NotContain(PluginViews.OpenUrlFromProperty);
        PluginViews.RouteProperties.Should().NotContain(PluginViews.OpenUrlFromProperty);
    }

    /// <summary>Simple names of letters, digits and underscores are accepted.</summary>
    /// <param name="value">The field name.</param>
    [Theory]
    [InlineData("url")]
    [InlineData("loginUrl")]
    [InlineData("login_url_2")]
    [InlineData("_x")]
    public void IsOpenUrlFieldName_Simple_IsAccepted(string value)
    {
        PluginViews.IsOpenUrlFieldName(value).Should().BeTrue();
    }

    /// <summary>Empty, dotted, spaced, hyphenated, bracketed and non ASCII values are refused.</summary>
    /// <param name="value">The value.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a.b")]
    [InlineData("a b")]
    [InlineData("a-b")]
    [InlineData("a[0]")]
    [InlineData("ürl")]
    public void IsOpenUrlFieldName_NotSimple_IsRefused(string? value)
    {
        PluginViews.IsOpenUrlFieldName(value).Should().BeFalse();
    }

    /// <summary>The longest allowed name passes and one character more is refused.</summary>
    [Fact]
    public void IsOpenUrlFieldName_Length_IsBounded()
    {
        PluginViews.IsOpenUrlFieldName(new string('a', PluginViews.MaxOpenUrlFieldLength)).Should().BeTrue();
        PluginViews.IsOpenUrlFieldName(new string('a', PluginViews.MaxOpenUrlFieldLength + 1)).Should().BeFalse();
    }
}
