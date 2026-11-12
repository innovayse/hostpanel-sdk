using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Payment;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Payment;

/// <summary>Tests of the optional <see cref="IPaymentReturnPlugin"/> capability.</summary>
public sealed class PaymentReturnPluginTests
{
    /// <summary>
    /// A plugin without the capability is still a valid payment plugin and is never asked to complete.
    /// </summary>
    [Fact]
    public void LegacyPlugin_IsNotAReturnPlugin()
    {
        IPaymentPlugin plugin = new LegacyPaymentPlugin();

        plugin.Should().NotBeAssignableTo<IPaymentReturnPlugin>();
    }

    /// <summary>A return plugin is still a payment plugin, found by the same resolver.</summary>
    [Fact]
    public void ReturnPlugin_IsAPaymentPlugin()
    {
        typeof(IPaymentPlugin).IsAssignableFrom(typeof(IPaymentReturnPlugin)).Should().BeTrue();
    }

    /// <summary>The manifest capability has its documented name.</summary>
    [Fact]
    public void Capability_HasItsName()
    {
        PluginCapabilities.PaymentReturn.Should().Be("paymentReturn");
    }

    /// <summary>The method takes a read only dictionary and answers a status.</summary>
    [Fact]
    public void CompleteAsync_HasTheDocumentedShape()
    {
        var method = typeof(IPaymentReturnPlugin).GetMethod(nameof(IPaymentReturnPlugin.CompleteAsync))!;

        method.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(string), typeof(IReadOnlyDictionary<string, string>), typeof(CancellationToken));
        method.ReturnType.Should().Be(typeof(Task<GatewayPaymentStatus>));
    }

    /// <summary>A nonce style gateway is paid once the nonce arrived, and the status read afterwards agrees.</summary>
    [Fact]
    public async Task CompleteAsync_WithNonce_PaysTheOrder()
    {
        var plugin = new NoncePaymentPlugin();
        var parameters = new Dictionary<string, string> { [NoncePaymentPlugin.NonceName] = "nonce-1" };

        var before = await plugin.GetStatusAsync("gw-1", CancellationToken.None);
        var completed = await plugin.CompleteAsync("gw-1", parameters, CancellationToken.None);
        var after = await plugin.GetStatusAsync("gw-1", CancellationToken.None);

        before.State.Should().Be(GatewayPaymentState.Pending);
        completed.State.Should().Be(GatewayPaymentState.Paid);
        after.Should().Be(completed);
    }

    /// <summary>A return without the value leaves the payment pending, and a repeat does not charge twice.</summary>
    [Fact]
    public async Task CompleteAsync_RepeatedOrEmpty_IsIdempotent()
    {
        var plugin = new NoncePaymentPlugin();
        var parameters = new Dictionary<string, string> { [NoncePaymentPlugin.NonceName] = "nonce-1" };

        var empty = await plugin.CompleteAsync("gw-2", new Dictionary<string, string>(), CancellationToken.None);
        await plugin.CompleteAsync("gw-2", parameters, CancellationToken.None);
        await plugin.CompleteAsync("gw-2", parameters, CancellationToken.None);

        empty.State.Should().Be(GatewayPaymentState.Pending);
        plugin.Charges.Should().Be(1);
    }

    /// <summary>The limits are pinned: a change is a deliberate decision, because hosts rely on them.</summary>
    [Fact]
    public void PaymentReturnLimits_Values_ArePinned()
    {
        PaymentReturnLimits.MaxParameters.Should().Be(32);
        PaymentReturnLimits.MaxNameLength.Should().Be(64);
        PaymentReturnLimits.MaxValueLength.Should().Be(2048);
    }
}
