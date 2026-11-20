using Hostpanel.Sdk.Plugins.Host;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Host;

/// <summary>Pins the shape of <see cref="IPluginHostServiceConnectionApi"/> and the way a plugin detects and uses it.</summary>
public sealed class PluginHostServiceConnectionApiTests
{
    /// <summary>The capability extends the host API and declares exactly one abstract operation, so the host API itself is unchanged.</summary>
    [Fact]
    public void ServiceConnectionApi_ExtendsHostApi_WithOneAbstractOperation()
    {
        var api = typeof(IPluginHostServiceConnectionApi);

        api.GetInterfaces().Should().Contain(typeof(IPluginHostApi));
        api.GetMethods().Select(method => method.Name).Should().Equal("ResolveServiceConnectionAsync");
        api.GetMethods().Should().OnlyContain(method => method.IsAbstract);
    }

    /// <summary>The older host API does not gain the operation, so a plugin built on it still loads.</summary>
    [Fact]
    public void HostApi_DoesNotDeclareTheOperation()
    {
        typeof(IPluginHostApi).GetMethod("ResolveServiceConnectionAsync").Should().BeNull();
    }

    /// <summary>A plugin detects the capability with a type test: a plain host lacks it, a host that implements it has it.</summary>
    [Fact]
    public void ServiceConnectionApi_TypeTest_SeparatesHostsThatHaveItFromThoseThatDoNot()
    {
        IPluginHostApi plain = new StubHostApi();
        IPluginHostApi capable = new StubServiceConnectionApi(Context());

        (plain is IPluginHostServiceConnectionApi).Should().BeFalse();
        (capable is IPluginHostServiceConnectionApi).Should().BeTrue();
    }

    /// <summary>A plugin passes only the service id and gets the server and the provisioning reference back.</summary>
    [Fact]
    public async Task ResolveServiceConnectionAsync_ReturnsTheServerAndTheReference()
    {
        var stub = new StubServiceConnectionApi(Context());
        IPluginHostApi host = stub;

        var context = await ((IPluginHostServiceConnectionApi)host).ResolveServiceConnectionAsync(42, CancellationToken.None);

        stub.LastServiceId.Should().Be(42);
        context.Server.Hostname.Should().Be("panel.example.test");
        context.Service.ServiceId.Should().Be(42);
        context.Service.ProvisioningRef.Should().Be("acct-7");
    }

    /// <summary>The answer carries server credentials, and its printed form still masks them.</summary>
    [Fact]
    public void ServiceConnection_ToString_MasksTheServerSecrets()
    {
        var printed = Context().ToString();

        printed.Should().NotContain("token-value");
        printed.Should().Contain("panel.example.test");
    }

    /// <summary>Builds the context the stub answers.</summary>
    /// <returns>A server with a token and a service with a provisioning reference.</returns>
    private static PanelAccountContext Context()
    {
        var server = new PanelServerConnection(1, "main", "panel.example.test", null, null, true, "api", null, "token-value");
        return new PanelAccountContext(server, new PanelService(42, "acct-7"));
    }
}
