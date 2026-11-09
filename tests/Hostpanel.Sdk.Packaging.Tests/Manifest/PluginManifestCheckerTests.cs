using Hostpanel.Sdk.Packaging.Enums;
using Hostpanel.Sdk.Packaging.Manifest;
using Hostpanel.Sdk.Packaging.Tests.Support;
using Hostpanel.Sdk.Plugins.Manifest;

namespace Hostpanel.Sdk.Packaging.Tests.Manifest;

/// <summary>Tests of <see cref="PluginManifestChecker"/>: the structural manifest rules that need no host state.</summary>
public sealed class PluginManifestCheckerTests
{
    /// <summary>The host's decisions the tests use: the default manifest versions and two client permission names.</summary>
    private static readonly PluginManifestOptions Options = new()
    {
        ClientPermissionNames = new HashSet<string>(StringComparer.Ordinal) { "ViewServices", "PlaceNewOrders" },
    };

    /// <summary>Checks a manifest made of the required members plus extra ones.</summary>
    /// <param name="extraMembers">More JSON members, without braces.</param>
    /// <returns>The violation, or <see langword="null"/>.</returns>
    private static PluginManifestViolation? Check(string extraMembers)
    {
        return PluginManifestChecker.Validate(ManifestJson.Read(extraMembers), Options);
    }

    /// <summary>Asserts that the extra members break exactly one rule.</summary>
    /// <param name="extraMembers">More JSON members, without braces.</param>
    /// <param name="rule">The rule expected.</param>
    private static void AssertBreaks(string extraMembers, PluginManifestRule rule)
    {
        Check(extraMembers)!.Rule.Should().Be(rule);
    }

    /// <summary>A manifest with nothing beyond the required members meets every rule, as a version 1 manifest does.</summary>
    [Fact]
    public void Validate_MinimalManifest_Passes()
    {
        Check(string.Empty).Should().BeNull();
    }

    /// <summary>A version 2 manifest that uses every block correctly passes.</summary>
    [Fact]
    public void Validate_FullManifest_Passes()
    {
        const string members = """
            "manifestVersion": 2,
            "settings": [{"key":"apiKey","label":"settings.apiKey","type":"Text"}],
            "locales": {"en": {"menu.reports":"Reports","page.reports":"Reports","settings.apiKey":"API key","widget.card":"Card"}},
            "api": {
              "client": [{"method":"Get","path":"/orders/{id}","permission":"ViewServices"},{"method":"Post","path":"/orders","permission":"Any"}],
              "admin": [{"method":"Get","path":"/orders"}],
              "callbacks": [{"method":"Post","path":"/notify"}]
            },
            "ui": {
              "client": {"menus":[{"id":"reports","label":"menu.reports","page":"reports","icon":"chart-bar"}],
                         "pages":[{"id":"reports","path":"/reports","title":"page.reports","view":"views/reports.json"}]},
              "admin": {"pages":[{"id":"overview","path":"/overview","title":"Plain title","view":"views/overview.json"}]},
              "slots": [{"id":"card","slot":"client.service.tabs","view":"views/card.json","title":"widget.card","contextKeys":["serviceId"],"permission":"Any"}]
            }
            """;

        Check(members).Should().BeNull();
    }

    /// <summary>A manifest version the host does not read is refused, and the host may widen the set.</summary>
    [Fact]
    public void Validate_ManifestVersion_FollowsTheHostsSet()
    {
        AssertBreaks("\"manifestVersion\": 3", PluginManifestRule.ManifestVersionUnknown);

        var widened = new PluginManifestOptions
        {
            SupportedManifestVersions = new HashSet<int> { 1, 2, 3 },
            ClientPermissionNames = new HashSet<string>(),
        };
        PluginManifestChecker.Validate(ManifestJson.Read("\"manifestVersion\": 3"), widened).Should().BeNull();
    }

    /// <summary>An id that is not a plugin id, or is one the host reserves, is refused.</summary>
    /// <param name="id">The manifest id.</param>
    [Theory]
    [InlineData("Demo")]
    [InlineData("de mo")]
    [InlineData("ui")]
    [InlineData("slots")]
    [InlineData("-demo")]
    public void Validate_InvalidId_IsRefused(string id)
    {
        PluginManifestChecker.Validate(ManifestJson.ReadWith(id, "1.0.0"), Options)!.Rule.Should().Be(PluginManifestRule.PluginIdInvalid);
    }

    /// <summary>A version that is not a strict semantic version is refused.</summary>
    /// <param name="version">The manifest version.</param>
    [Theory]
    [InlineData("1.0")]
    [InlineData("v1.0.0")]
    [InlineData("01.0.0")]
    public void Validate_InvalidVersion_IsRefused(string version)
    {
        PluginManifestChecker.Validate(ManifestJson.ReadWith("demo", version), Options)!.Rule.Should().Be(PluginManifestRule.VersionInvalid);
    }

    /// <summary>A client permission must be the any-permission marker or a name the host defines; the case must match.</summary>
    [Fact]
    public void Validate_ClientPermission_MustBeKnown()
    {
        AssertBreaks(
            "\"api\":{\"client\":[{\"method\":\"Get\",\"path\":\"/a\",\"permission\":\"Fly\"}]}", PluginManifestRule.PermissionUnknown);
        AssertBreaks(
            "\"api\":{\"client\":[{\"method\":\"Get\",\"path\":\"/a\",\"permission\":\"viewservices\"}]}", PluginManifestRule.PermissionUnknown);
        Check("\"api\":{\"client\":[{\"method\":\"Get\",\"path\":\"/a\",\"permission\":\"PlaceNewOrders\"}]}").Should().BeNull();
    }

    /// <summary>The admin area takes no permission, for a route, a page or a slot.</summary>
    [Fact]
    public void Validate_AdminPermission_IsRefused()
    {
        AssertBreaks(
            "\"api\":{\"admin\":[{\"method\":\"Get\",\"path\":\"/a\",\"permission\":\"Any\"}]}", PluginManifestRule.AdminPermissionDeclared);
        AssertBreaks(
            "\"ui\":{\"admin\":{\"pages\":[{\"id\":\"p\",\"path\":\"/p\",\"title\":\"T\",\"view\":\"v.json\",\"permission\":\"Any\"}]}}",
            PluginManifestRule.AdminPermissionDeclared);
        AssertBreaks(
            "\"ui\":{\"slots\":[{\"id\":\"w\",\"slot\":\"admin.client.panels\",\"view\":\"v.json\",\"permission\":\"Any\"}]}",
            PluginManifestRule.AdminPermissionDeclared);
    }

    /// <summary>A route path must be a slash plus lowercase literals and parameters.</summary>
    /// <param name="path">The declared path.</param>
    [Theory]
    [InlineData("orders")]
    [InlineData("/Orders")]
    [InlineData("/orders/..")]
    [InlineData("/")]
    [InlineData("/orders/")]
    public void Validate_RoutePath_IsChecked(string path)
    {
        AssertBreaks(
            "\"api\":{\"client\":[{\"method\":\"Get\",\"path\":\"" + path + "\"}]}", PluginManifestRule.RoutePathInvalid);
    }

    /// <summary>A method and path template is unique within an area, parameter names ignored; another method or another area is fine.</summary>
    [Fact]
    public void Validate_DuplicateRoute_IsRefusedWithinAnArea()
    {
        AssertBreaks(
            "\"api\":{\"client\":[{\"method\":\"Get\",\"path\":\"/a/{id}\"},{\"method\":\"Get\",\"path\":\"/a/{name}\"}]}",
            PluginManifestRule.RouteDuplicate);
        Check("\"api\":{\"client\":[{\"method\":\"Get\",\"path\":\"/a\"},{\"method\":\"Post\",\"path\":\"/a\"}],\"admin\":[{\"method\":\"Get\",\"path\":\"/a\"}]}")
            .Should().BeNull();
    }

    /// <summary>A callback route carries no permission and does not stream.</summary>
    [Fact]
    public void Validate_Callbacks_FollowTheSdkRule()
    {
        AssertBreaks(
            "\"api\":{\"callbacks\":[{\"method\":\"Post\",\"path\":\"/n\",\"permission\":\"Any\"}]}", PluginManifestRule.CallbackInvalid);
        AssertBreaks(
            "\"api\":{\"callbacks\":[{\"method\":\"Post\",\"path\":\"/n\",\"streaming\":true}]}", PluginManifestRule.CallbackInvalid);
        AssertBreaks(
            "\"api\":{\"callbacks\":[{\"method\":\"Post\",\"path\":\"/n\"},{\"method\":\"Post\",\"path\":\"/n\"}]}", PluginManifestRule.RouteDuplicate);
    }

    /// <summary>A page id must be a page id and unique in its area, and a page path follows the route path rule.</summary>
    [Fact]
    public void Validate_Pages_AreChecked()
    {
        const string page = "{\"id\":\"p\",\"path\":\"/p\",\"title\":\"T\",\"view\":\"v.json\"}";

        AssertBreaks(
            "\"ui\":{\"client\":{\"pages\":[{\"id\":\"By-Path\",\"path\":\"/p\",\"title\":\"T\",\"view\":\"v.json\"}]}}", PluginManifestRule.PageIdInvalid);
        AssertBreaks(
            "\"ui\":{\"client\":{\"pages\":[{\"id\":\"by-path\",\"path\":\"/p\",\"title\":\"T\",\"view\":\"v.json\"}]}}", PluginManifestRule.PageIdInvalid);
        AssertBreaks("\"ui\":{\"client\":{\"pages\":[" + page + "," + page + "]}}", PluginManifestRule.PageIdInvalid);
        AssertBreaks(
            "\"ui\":{\"client\":{\"pages\":[{\"id\":\"p\",\"path\":\"P\",\"title\":\"T\",\"view\":\"v.json\"}]}}", PluginManifestRule.RoutePathInvalid);
        Check("\"ui\":{\"client\":{\"pages\":[" + page + "]},\"admin\":{\"pages\":[" + page + "]}}").Should().BeNull();
    }

    /// <summary>A menu icon is a plain name of lowercase letters, digits and hyphens, never a URL or markup.</summary>
    /// <param name="icon">The icon value.</param>
    [Theory]
    [InlineData("https://example.org/i.svg")]
    [InlineData("Chart")]
    [InlineData("a b")]
    [InlineData("<svg>")]
    public void Validate_MenuIcon_MustBeAPlainName(string icon)
    {
        AssertBreaks(
            "\"ui\":{\"client\":{\"menus\":[{\"id\":\"m\",\"label\":\"Menu\",\"page\":\"p\",\"icon\":\"" + icon + "\"}]}}",
            PluginManifestRule.MenuIconInvalid);
    }

    /// <summary>A slot entry names a slot that exists, a safe widget id once per slot, and only context keys its slot offers.</summary>
    [Fact]
    public void Validate_Slots_AreChecked()
    {
        const string valid = "{\"id\":\"w\",\"slot\":\"client.dashboard.cards\",\"view\":\"v.json\"}";

        AssertBreaks("\"ui\":{\"slots\":[{\"id\":\"w\",\"slot\":\"nowhere\",\"view\":\"v.json\"}]}", PluginManifestRule.SlotUnknown);
        AssertBreaks("\"ui\":{\"slots\":[{\"id\":\"W!\",\"slot\":\"client.dashboard.cards\",\"view\":\"v.json\"}]}", PluginManifestRule.SlotWidgetIdInvalid);
        AssertBreaks("\"ui\":{\"slots\":[" + valid + "," + valid + "]}", PluginManifestRule.SlotWidgetDuplicate);
        AssertBreaks(
            "\"ui\":{\"slots\":[{\"id\":\"w\",\"slot\":\"client.dashboard.cards\",\"view\":\"v.json\",\"contextKeys\":[\"serviceId\"]}]}",
            PluginManifestRule.SlotContextKeyNotOffered);
        Check("\"ui\":{\"slots\":[" + valid + ",{\"id\":\"w\",\"slot\":\"client.service.tabs\",\"view\":\"v.json\",\"contextKeys\":[\"serviceId\"]}]}")
            .Should().BeNull();
    }

    /// <summary>A label shaped like a text key must be defined in the English texts; plain text needs no entry.</summary>
    [Fact]
    public void Validate_TextKeys_MustBeDefinedInEnglish()
    {
        const string menu = "\"ui\":{\"client\":{\"menus\":[{\"id\":\"m\",\"label\":\"menu.reports\",\"page\":\"p\"}]}}";

        AssertBreaks(menu, PluginManifestRule.LabelKeyMissing);
        AssertBreaks(menu + ",\"locales\":{\"ru\":{\"menu.reports\":\"Otchety\"}}", PluginManifestRule.LabelKeyMissing);
        Check(menu + ",\"locales\":{\"en\":{\"menu.reports\":\"Reports\"}}").Should().BeNull();
        Check("\"ui\":{\"client\":{\"menus\":[{\"id\":\"m\",\"label\":\"Reports\",\"page\":\"p\"}]}}").Should().BeNull();
    }

    /// <summary>A setting description that merely looks like a key (a host name) is text, unless some locale defines that exact value.</summary>
    [Fact]
    public void Validate_SettingHelpText_IsAKeyOnlyWhenALocaleDefinesIt()
    {
        const string setting = "\"settings\":[{\"key\":\"host\",\"label\":\"Host\",\"type\":\"Text\",\"placeholder\":\"api.example.org\"}]";

        Check(setting).Should().BeNull();
        AssertBreaks(setting + ",\"locales\":{\"ru\":{\"api.example.org\":\"x\"}}", PluginManifestRule.LabelKeyMissing);
        Check(setting + ",\"locales\":{\"ru\":{\"api.example.org\":\"x\"},\"en\":{\"api.example.org\":\"x\"}}").Should().BeNull();
    }

    /// <summary>The shape test for text keys is exact: dot separated words, no spaces, nothing after.</summary>
    /// <param name="value">The label value.</param>
    /// <param name="expected">Whether it is shaped like a key.</param>
    [Theory]
    [InlineData("menu.reports", true)]
    [InlineData("a.b.c", true)]
    [InlineData("Reports", false)]
    [InlineData("two words.here", false)]
    [InlineData("menu.reports\n", false)]
    [InlineData(".menu", false)]
    public void IsTextKey_FollowsTheShape(string value, bool expected)
    {
        PluginManifestChecker.IsTextKey(value).Should().Be(expected);
    }

    /// <summary>The English texts are found whatever the case of the locale name, and absent when the manifest ships none.</summary>
    [Fact]
    public void EnglishTexts_FindsTheEnglishLocale()
    {
        PluginManifestChecker.EnglishTexts(ManifestJson.Read("\"locales\":{\"EN\":{\"a.b\":\"x\"}}"))!.Should().ContainKey("a.b");
        PluginManifestChecker.EnglishTexts(ManifestJson.Read("\"locales\":{\"ru\":{\"a.b\":\"x\"}}")).Should().BeNull();
    }

    /// <summary>An assembly built against the SDK major version the host provides is accepted, and an older one is refused with a rebuild message.</summary>
    [Fact]
    public void ValidateSdkReference_FollowsTheHostsMajorVersion()
    {
        using var folder = new TempFolder();
        var assembly = typeof(ManifestJson).Assembly.Location;
        File.Copy(assembly, Path.Combine(folder.Path, "Acme.Demo.dll"));
        var referenced = typeof(PluginManifest).Assembly.GetName().Version!.Major;

        PluginManifestChecker.ValidateSdkReference(folder.Path).Should().BeNull();
        PluginManifestChecker.ValidateSdkReference(folder.Path, referenced).Should().BeNull();
        var violation = PluginManifestChecker.ValidateSdkReference(folder.Path, referenced + 1);

        violation!.Rule.Should().Be(PluginManifestRule.SdkAssemblyOutdated);
        violation.Detail.Should().Contain("Acme.Demo.dll").And.NotContain(folder.Path);
    }

    /// <summary>A file that is not a managed assembly cannot have been built against the SDK, so it is not refused here.</summary>
    [Fact]
    public void ValidateSdkReference_NativeOrGarbageFile_IsIgnored()
    {
        using var folder = new TempFolder();
        folder.Write("native.dll", "this is not an assembly");

        PluginManifestChecker.ValidateSdkReference(folder.Path, 99).Should().BeNull();
    }

    /// <summary>A plugin folder that does not exist is a refusal of the plugin, not an exception.</summary>
    [Fact]
    public void ValidateSdkReference_MissingFolder_IsAssemblyUnreadable()
    {
        using var folder = new TempFolder();
        var missing = Path.Combine(folder.Path, "no-such-folder");

        var violation = PluginManifestChecker.ValidateSdkReference(missing, 3);

        violation!.Rule.Should().Be(PluginManifestRule.AssemblyUnreadable);
        violation.Detail.Should().NotContain(folder.Path);
    }

    /// <summary>A manifest with no <c>tags</c> reads an empty list and passes.</summary>
    [Fact]
    public void Validate_NoTags_PassesWithEmptyList()
    {
        ManifestJson.Read().Tags.Should().BeEmpty();
        Check(string.Empty).Should().BeNull();
    }

    /// <summary>A <c>null</c> <c>tags</c> reads as an empty list and passes.</summary>
    [Fact]
    public void Validate_NullTags_PassesWithEmptyList()
    {
        ManifestJson.Read("\"tags\": null").Tags.Should().BeEmpty();
        Check("\"tags\": null").Should().BeNull();
    }

    /// <summary>Well formed tags are read in order and pass, including the shortest, the longest and a full list.</summary>
    [Fact]
    public void Validate_GoodTags_ReadAndPass()
    {
        var longest = new string('a', 30);
        ManifestJson.Read("\"tags\": [\"backup\", \"billing-2\", \"ab\", \"" + longest + "\"]").Tags
            .Should().Equal("backup", "billing-2", "ab", longest);
        Check("\"tags\": [\"backup\", \"billing-2\", \"ab\", \"" + longest + "\"]").Should().BeNull();
        Check("\"tags\": [" + string.Join(",", Enumerable.Range(0, 10).Select(i => "\"tag-" + i + "\"")) + "]").Should().BeNull();
    }

    /// <summary>A tag that is too short, too long, upper case, has a leading hash, a space or another symbol breaks the tags rule.</summary>
    /// <param name="tag">The tag.</param>
    [Theory]
    [InlineData("a")]
    [InlineData("")]
    [InlineData("#backup")]
    [InlineData("Backup")]
    [InlineData("back up")]
    [InlineData("back_up")]
    public void Validate_BadTag_BreaksTagsInvalid(string tag)
    {
        AssertBreaks("\"tags\": [\"" + tag + "\"]", PluginManifestRule.TagsInvalid);
    }

    /// <summary>A tag of 31 characters breaks the tags rule.</summary>
    [Fact]
    public void Validate_TagTooLong_BreaksTagsInvalid()
    {
        AssertBreaks("\"tags\": [\"" + new string('a', 31) + "\"]", PluginManifestRule.TagsInvalid);
    }

    /// <summary>More than ten tags break the tags rule.</summary>
    [Fact]
    public void Validate_MoreThanTenTags_BreaksTagsInvalid()
    {
        AssertBreaks("\"tags\": [" + string.Join(",", Enumerable.Range(0, 11).Select(i => "\"tag-" + i + "\"")) + "]", PluginManifestRule.TagsInvalid);
    }

    /// <summary>The same tag twice breaks the tags rule.</summary>
    [Fact]
    public void Validate_DuplicateTag_BreaksTagsInvalid()
    {
        AssertBreaks("\"tags\": [\"backup\", \"backup\"]", PluginManifestRule.TagsInvalid);
    }

    /// <summary>A tag with a trailing newline breaks the tags rule, so a line break cannot ride along in a keyword.</summary>
    [Fact]
    public void Validate_TagWithTrailingNewline_BreaksTagsInvalid()
    {
        AssertBreaks("\"tags\": [\"backup\\n\"]", PluginManifestRule.TagsInvalid);
    }

    /// <summary>A manifest without the host range and changelog reads them as null and stays valid.</summary>
    [Fact]
    public void Validate_NoHostRangeOrChangelog_ReadsNullAndPasses()
    {
        var manifest = ManifestJson.Read();
        manifest.MinHostVersion.Should().BeNull();
        manifest.MaxHostVersion.Should().BeNull();
        manifest.Changelog.Should().BeNull();
        Check(string.Empty).Should().BeNull();
    }

    /// <summary>The host range and changelog are read from their camelCase members and pass when well formed.</summary>
    [Fact]
    public void Validate_HostRangeAndChangelog_ReadAndPass()
    {
        const string members = "\"minHostVersion\": \"2.0.0\", \"maxHostVersion\": \"3.1.0-preview.1\", \"changelog\": \"Fixes the export.\"";
        var manifest = ManifestJson.Read(members);
        manifest.MinHostVersion.Should().Be("2.0.0");
        manifest.MaxHostVersion.Should().Be("3.1.0-preview.1");
        manifest.Changelog.Should().Be("Fixes the export.");
        Check(members).Should().BeNull();
    }

    /// <summary>Equal bounds, or only one bound, pass.</summary>
    /// <param name="members">The JSON members holding the range.</param>
    [Theory]
    [InlineData("\"minHostVersion\": \"2.0.0\", \"maxHostVersion\": \"2.0.0\"")]
    [InlineData("\"minHostVersion\": \"2.0.0\"")]
    [InlineData("\"maxHostVersion\": \"2.0.0\"")]
    public void Validate_OpenOrEqualRange_Passes(string members)
    {
        Check(members).Should().BeNull();
    }

    /// <summary>A bound that is not a semantic version breaks the host version rule.</summary>
    /// <param name="member">The member name.</param>
    /// <param name="value">The bad version.</param>
    [Theory]
    [InlineData("minHostVersion", "2.0")]
    [InlineData("minHostVersion", "v2.0.0")]
    [InlineData("maxHostVersion", "")]
    [InlineData("maxHostVersion", "02.0.0")]
    public void Validate_BadHostVersion_BreaksHostVersionInvalid(string member, string value)
    {
        AssertBreaks("\"" + member + "\": \"" + value + "\"", PluginManifestRule.HostVersionInvalid);
    }

    /// <summary>A lower bound above the upper bound breaks the range rule, by semantic precedence.</summary>
    [Fact]
    public void Validate_MinAboveMax_BreaksHostVersionRangeInverted()
    {
        AssertBreaks("\"minHostVersion\": \"2.10.0\", \"maxHostVersion\": \"2.9.0\"", PluginManifestRule.HostVersionRangeInverted);
        AssertBreaks("\"minHostVersion\": \"2.0.0\", \"maxHostVersion\": \"2.0.0-preview.1\"", PluginManifestRule.HostVersionRangeInverted);
    }

    /// <summary>A changelog of exactly the limit passes; one character more breaks the changelog rule.</summary>
    [Fact]
    public void Validate_ChangelogLength_LimitPassesAndLongerBreaks()
    {
        Check("\"changelog\": \"" + new string('a', PluginManifest.MaxChangelogLength) + "\"").Should().BeNull();
        AssertBreaks("\"changelog\": \"" + new string('a', PluginManifest.MaxChangelogLength + 1) + "\"", PluginManifestRule.ChangelogTooLong);
    }
}
