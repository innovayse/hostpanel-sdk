using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Provisioning;

/// <summary>Tests of <see cref="PanelServerAddress"/>: the scheme, the port, the IPv6 brackets and the refusal of a malformed host.</summary>
public sealed class PanelServerAddressTests
{
    /// <summary>The port the saved server carries wins over the plugin's default, and the scheme follows the SSL setting.</summary>
    [Fact]
    public void BaseUrl_UsesTheSavedPortAndScheme()
    {
        PanelServerAddress.BaseUrl(PanelFixtures.Server, 2304).Should().Be("https://panel.example.test:2087");
        PanelServerAddress.BaseUrl(PanelFixtures.Server with { UseSsl = false }, 2304).Should().Be("http://panel.example.test:2087");
    }

    /// <summary>A server with no saved port gets the plugin's own default, because the host never guesses one.</summary>
    [Fact]
    public void BaseUrl_WithoutASavedPort_UsesTheDefault()
    {
        PanelServerAddress.BaseUrl(PanelFixtures.PasswordServer, 2304).Should().Be("https://panel2.example.test:2304");
    }

    /// <summary>A bare IPv6 literal is written in brackets, an already bracketed one stays as it is, and an IPv4 address is unchanged.</summary>
    [Theory]
    [InlineData("2001:db8::1", "https://[2001:db8::1]:8443")]
    [InlineData("[2001:db8::1]", "https://[2001:db8::1]:8443")]
    [InlineData("192.0.2.10", "https://192.0.2.10:8443")]
    public void BaseUrl_FormatsTheHost(string hostname, string expected)
    {
        var server = PanelFixtures.Server with { Hostname = hostname, Port = 8443 };

        PanelServerAddress.BaseUrl(server, 2304).Should().Be(expected);
    }

    /// <summary>The URI form has the same address and resolves a path against the root.</summary>
    [Fact]
    public void BaseUri_ResolvesPathsAgainstTheAddress()
    {
        var baseUri = PanelServerAddress.BaseUri(PanelFixtures.Server, 2304);

        new Uri(baseUri, "/api/ping").ToString().Should().Be("https://panel.example.test:2087/api/ping");
    }

    /// <summary>A host name that carries a path, credentials, a query, white space or a port is refused, so a credential is never sent to an address the operator did not save.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("panel.example.test/path")]
    [InlineData("user@panel.example.test")]
    [InlineData("panel.example.test?x=1")]
    [InlineData("panel.example.test#frag")]
    [InlineData("panel example.test")]
    [InlineData("panel.example.test:2087")]
    [InlineData("[panel.example.test]")]
    [InlineData("[192.0.2.10]")]
    [InlineData("fe80::1%eth0")]
    public void BaseUrl_MalformedHost_Throws(string hostname)
    {
        var run = () => PanelServerAddress.BaseUrl(PanelFixtures.Server with { Hostname = hostname }, 2304);

        run.Should().Throw<ArgumentException>().WithParameterName("server");
    }

    /// <summary>A saved port outside 1 to 65535 and a default outside it are refused.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void BaseUrl_PortOutOfRange_Throws(int port)
    {
        var savedPort = () => PanelServerAddress.BaseUrl(PanelFixtures.Server with { Port = port }, 2304);
        var defaultPort = () => PanelServerAddress.BaseUrl(PanelFixtures.PasswordServer, port);

        savedPort.Should().Throw<ArgumentException>();
        defaultPort.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>A missing server is refused with the usual argument exception.</summary>
    [Fact]
    public void BaseUrl_NullServer_Throws()
    {
        var run = () => PanelServerAddress.BaseUrl(null!, 2304);

        run.Should().Throw<ArgumentNullException>();
    }
}
