using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Host;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Host;

/// <summary>Pins the hosting access kinds, the records of the hosting API and the shape of <see cref="IPluginHostHostingApi"/>.</summary>
public sealed class PluginHostingApiTests
{
    /// <summary>Every kind is listed once, in the order the admin panel shows them, and each keeps its manifest word.</summary>
    [Fact]
    public void HostAccess_All_ListsEveryKindInOrder()
    {
        PluginHostAccess.All.Should().Equal("ssh", "websites", "databases", "serviceConnection");
        PluginHostAccess.Ssh.Should().Be("ssh");
        PluginHostAccess.Websites.Should().Be("websites");
        PluginHostAccess.Databases.Should().Be("databases");
        PluginHostAccess.ServiceConnection.Should().Be("serviceConnection");
    }

    /// <summary>The new failures are appended, so the values released before keep their numbers.</summary>
    [Fact]
    public void HostAccessFailure_NewValues_AreAppended()
    {
        ((int)PluginHostAccessFailure.ServerUnavailable).Should().Be(4);
        ((int)PluginHostAccessFailure.InvalidRequest).Should().Be(5);
        ((int)PluginHostAccessFailure.Conflict).Should().Be(6);
    }

    /// <summary>The printed form of a database never shows the password, and the other members stay readable.</summary>
    [Fact]
    public void PluginDatabase_ToString_MasksThePassword()
    {
        var database = new PluginDatabase(Guid.NewGuid(), "site_db", "site_user", "db.example.test", 3306, "s3cret-value");

        var text = database.ToString();

        text.Should().NotContain("s3cret-value");
        text.Should().Contain("site_db").And.Contain("site_user").And.Contain("db.example.test");
    }

    /// <summary>A database answered after creation carries no password, and prints without a failure.</summary>
    [Fact]
    public void PluginDatabase_NoPassword_PrintsAndKeepsNull()
    {
        var database = new PluginDatabase(Guid.NewGuid(), "site_db", "site_user", "db.example.test", 3306, null);

        database.Password.Should().BeNull();
        database.ToString().Should().Contain("site_db");
    }

    /// <summary>The website specification and the website keep the values they were built with.</summary>
    [Fact]
    public void WebsiteRecords_KeepTheirValues()
    {
        var id = Guid.NewGuid();
        var spec = new PluginWebsiteSpec("shop.example.test", "sites/shop", "8.3", true);
        var website = new PluginWebsite(id, spec.Hostname, spec.DocumentRoot, "/home/acct/sites/shop", spec.PhpVersion);

        spec.DocumentRootMayBeLink.Should().BeTrue();
        website.Id.Should().Be(id);
        website.DocumentRootPath.Should().Be("/home/acct/sites/shop");
        website.PhpVersion.Should().Be("8.3");
    }

    /// <summary>The capabilities keep the flags and the PHP versions they were built with.</summary>
    [Fact]
    public void HostingCapabilities_KeepTheirValues()
    {
        var capabilities = new PluginHostingCapabilities(true, false, true, ["8.2", "8.3"]);

        capabilities.Ssh.Should().BeTrue();
        capabilities.Websites.Should().BeFalse();
        capabilities.Databases.Should().BeTrue();
        capabilities.PhpVersions.Should().Equal("8.2", "8.3");
    }

    /// <summary>The hosting API extends the host API and declares exactly its five operations with no default body.</summary>
    [Fact]
    public void HostingApi_ExtendsHostApi_WithFiveAbstractOperations()
    {
        var api = typeof(IPluginHostHostingApi);

        api.GetInterfaces().Should().Contain(typeof(IPluginHostApi));
        api.GetMethods().Select(method => method.Name).Should().BeEquivalentTo(
            "GetHostingCapabilitiesAsync",
            "EnsureWebsiteAsync",
            "RemoveWebsiteAsync",
            "EnsureDatabaseAsync",
            "RemoveDatabaseAsync");
        api.GetMethods().Should().OnlyContain(method => method.IsAbstract);
    }

    /// <summary>A plugin detects the capability with a type test: a host without it is not the sub-interface, one with it is.</summary>
    [Fact]
    public void HostingApi_TypeTest_SeparatesHostsThatHaveItFromThoseThatDoNot()
    {
        IPluginHostApi plain = new StubHostApi();
        IPluginHostApi hosting = new StubHostingApi();

        (plain is IPluginHostHostingApi).Should().BeFalse();
        (hosting is IPluginHostHostingApi).Should().BeTrue();
    }
}
