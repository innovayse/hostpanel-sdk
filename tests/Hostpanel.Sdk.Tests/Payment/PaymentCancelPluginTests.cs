using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Payment;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Payment;

/// <summary>Tests of the optional <see cref="IPaymentCancelPlugin"/> capability.</summary>
public sealed class PaymentCancelPluginTests
{
    /// <summary>A plugin without the capability is not a cancel plugin, so the host never asks it to cancel.</summary>
    [Fact]
    public void LegacyPlugin_IsNotACancelPlugin()
    {
        IPaymentPlugin plugin = new LegacyPaymentPlugin();

        plugin.Should().NotBeAssignableTo<IPaymentCancelPlugin>();
    }

    /// <summary>A cancel plugin is still a payment plugin, found by the same resolver.</summary>
    [Fact]
    public void CancelPlugin_IsAPaymentPlugin()
    {
        typeof(IPaymentPlugin).IsAssignableFrom(typeof(IPaymentCancelPlugin)).Should().BeTrue();
    }

    /// <summary>Cancelling twice answers cancelled, then already cancelled, and never fails.</summary>
    [Fact]
    public async Task CancelPaymentAsync_Repeated_IsIdempotent()
    {
        IPaymentCancelPlugin plugin = new RefundIdPaymentPlugin();

        var first = await plugin.CancelPaymentAsync("order-1", CancellationToken.None);
        var second = await plugin.CancelPaymentAsync("order-1", CancellationToken.None);

        first.Outcome.Should().Be(PaymentCancelOutcome.Cancelled);
        second.Outcome.Should().Be(PaymentCancelOutcome.AlreadyCancelled);
        second.GatewayReference.Should().Be(first.GatewayReference);
    }

    /// <summary>The outcome values start at 1 and 0 is not an outcome.</summary>
    [Fact]
    public void PaymentCancelOutcome_ValuesStartAtOne()
    {
        Enum.GetValues<PaymentCancelOutcome>().Select(v => (int)v).Should().Equal(1, 2, 3);
        Enum.IsDefined((PaymentCancelOutcome)0).Should().BeFalse();
    }
}
