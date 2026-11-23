using Hostpanel.Sdk.Plugins.Host;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Host;

/// <summary>Pins the shape of <see cref="IPluginHostSshSourceApi"/> and the new optional members of the hosting records.</summary>
public sealed class PluginHostSshSourceApiTests
{
    /// <summary>The capability extends the host API and declares exactly one abstract operation.</summary>
    [Fact]
    public void SshSourceApi_ExtendsHostApi_WithOneAbstractOperation()
    {
        var api = typeof(IPluginHostSshSourceApi);

        api.GetInterfaces().Should().Contain(typeof(IPluginHostApi));
        api.GetMethods().Select(method => method.Name).Should().Equal("IssueSshAccessAsync");
        api.GetMethods().Should().OnlyContain(method => method.IsAbstract);
    }

    /// <summary>The host API keeps its one issue operation, so a plugin built on it still loads.</summary>
    [Fact]
    public void HostApi_KeepsItsOwnIssueOperation()
    {
        typeof(IPluginHostApi).GetMethods().Count(method => method.Name == "IssueSshAccessAsync").Should().Be(1);
    }

    /// <summary>A plugin detects the capability with a type test and passes the sources it wants.</summary>
    [Fact]
    public async Task SshSourceApi_TypeTest_AndIssue_PassTheSources()
    {
        var stub = new StubSshSourceApi();
        IPluginHostApi host = stub;
        IPluginHostApi plain = new StubHostApi();

        (plain is IPluginHostSshSourceApi).Should().BeFalse();
        (host is IPluginHostSshSourceApi).Should().BeTrue();

        await ((IPluginHostSshSourceApi)host).IssueSshAccessAsync(7, "deploy", ["203.0.113.7", "198.51.100.0/24"], CancellationToken.None);

        stub.LastAllowedFrom.Should().Equal("203.0.113.7", "198.51.100.0/24");
    }

    /// <summary>The host key is null unless the host sets it, and a constructor call without it still compiles.</summary>
    [Fact]
    public void PluginSshAccess_HostKey_DefaultsToNullAndKeepsItsValue()
    {
        var without = new PluginSshAccess(Guid.NewGuid(), "ssh.example.test", 22, "acct", null, "key", "SHA256:abc");
        var with = without with { HostKey = "ssh-ed25519 AAAAC3" };

        without.HostKey.Should().BeNull();
        with.HostKey.Should().Be("ssh-ed25519 AAAAC3");
        with.HostKeyFingerprint.Should().Be("SHA256:abc");
    }

    /// <summary>The PHP binary path is null unless the host sets it, and a constructor call without it still compiles.</summary>
    [Fact]
    public void PluginWebsite_PhpCliPath_DefaultsToNullAndKeepsItsValue()
    {
        var without = new PluginWebsite(Guid.NewGuid(), "shop.example.test", "sites/shop", "/home/acct/sites/shop", "8.3");
        var with = without with { PhpCliPath = "/opt/php83/bin/php" };

        without.PhpCliPath.Should().BeNull();
        with.PhpCliPath.Should().Be("/opt/php83/bin/php");
        with.PhpVersion.Should().Be("8.3");
    }
}
