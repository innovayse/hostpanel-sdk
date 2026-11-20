using Hostpanel.Sdk.Plugins.Host;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Host;

/// <summary>Pins the shape of <see cref="IPluginHostPaymentApi"/> and the way a plugin detects and uses it.</summary>
public sealed class PluginHostPaymentApiTests
{
    /// <summary>The payment API extends the host API, which does not change, and declares exactly one abstract operation.</summary>
    [Fact]
    public void PaymentApi_ExtendsHostApi_WithOneAbstractOperation()
    {
        var api = typeof(IPluginHostPaymentApi);

        api.GetInterfaces().Should().Contain(typeof(IPluginHostApi));
        api.GetMethods().Select(method => method.Name).Should().Equal("RequestOrderReadAsync");
        api.GetMethods().Should().OnlyContain(method => method.IsAbstract);
    }

    /// <summary>The host API itself keeps its five operations, so a host and a plugin built on an earlier version are unaffected.</summary>
    [Fact]
    public void HostApi_IsUnchanged_FiveOperations()
    {
        typeof(IPluginHostApi).GetMethods().Select(method => method.Name).Should().BeEquivalentTo(
            "GetProfileAsync",
            "ListServicesAsync",
            "ListInvoicesAsync",
            "IssueSshAccessAsync",
            "RevokeSshAccessAsync");
    }

    /// <summary>A plugin detects the capability with a type test: a host without it is not the sub-interface, one with it is.</summary>
    [Fact]
    public void PaymentApi_TypeTest_SeparatesHostsThatHaveItFromThoseThatDoNot()
    {
        IPluginHostApi plain = new StubHostApi();
        IPluginHostApi payment = new RecordingPaymentHostApi();

        (plain is IPluginHostPaymentApi).Should().BeFalse();
        (payment is IPluginHostPaymentApi).Should().BeTrue();
    }

    /// <summary>A callback that finds the capability asks for the order it names, and a repeat is passed on as it is.</summary>
    [Fact]
    public async Task RequestOrderReadAsync_OnCapableHost_PassesTheOrderId()
    {
        var recording = new RecordingPaymentHostApi();
        IPluginHostApi host = recording;

        if (host is IPluginHostPaymentApi payment)
        {
            await payment.RequestOrderReadAsync("order-1", CancellationToken.None);
            await payment.RequestOrderReadAsync("order-1", CancellationToken.None);
        }

        recording.Requested.Should().Equal("order-1", "order-1");
    }
}
