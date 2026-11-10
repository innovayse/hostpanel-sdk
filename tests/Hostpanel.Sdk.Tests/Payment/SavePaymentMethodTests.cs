using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Payment;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Payment;

/// <summary>Tests of the optional save-the-method flag on <see cref="PaymentInstruction"/> and its answer on <see cref="GatewayPaymentStatus"/>.</summary>
public sealed class SavePaymentMethodTests
{
    /// <summary>An instruction built the old way asks for no save, so existing callers keep their behaviour.</summary>
    [Fact]
    public void PaymentInstruction_BuiltWithoutTheFlag_DoesNotAskToSave()
    {
        var instruction = new PaymentInstruction("order-1", 1000, "https://host.example/return", null, null);

        instruction.SavePaymentMethod.Should().BeFalse();
    }

    /// <summary>A status built the old way carries no saved method.</summary>
    [Fact]
    public void GatewayPaymentStatus_BuiltWithoutTheMethod_HasNone()
    {
        var status = new GatewayPaymentStatus(GatewayPaymentState.Paid, "tx-1", "PAID");

        status.SavedMethod.Should().BeNull();
    }

    /// <summary>A plugin that honours the flag returns the stored method once the payment is paid.</summary>
    [Fact]
    public async Task GetStatusAsync_FlagRequested_ReturnsTheSavedMethod()
    {
        IPaymentPlugin plugin = new SavingPaymentPlugin();
        var instruction = new PaymentInstruction("order-1", 1000, "https://host.example/return", null, null, SavePaymentMethod: true);

        var session = await plugin.CreatePaymentAsync(instruction, CancellationToken.None);
        var status = await plugin.GetStatusAsync(session.GatewayOrderId, CancellationToken.None);

        status.State.Should().Be(GatewayPaymentState.Paid);
        status.SavedMethod.Should().Be(SavingPaymentPlugin.StoredMethod);
    }

    /// <summary>Without the flag the same plugin returns no method, and the payment is still paid.</summary>
    [Fact]
    public async Task GetStatusAsync_FlagNotRequested_ReturnsNoMethod()
    {
        IPaymentPlugin plugin = new SavingPaymentPlugin();
        var instruction = new PaymentInstruction("order-2", 1000, "https://host.example/return", null, null);

        var session = await plugin.CreatePaymentAsync(instruction, CancellationToken.None);
        var status = await plugin.GetStatusAsync(session.GatewayOrderId, CancellationToken.None);

        status.State.Should().Be(GatewayPaymentState.Paid);
        status.SavedMethod.Should().BeNull();
    }
}
