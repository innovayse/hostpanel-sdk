using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Payment;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Payment;

/// <summary>
/// Tests of the optional save-the-method flag and customer on <see cref="PaymentInstruction"/> and its answer on
/// <see cref="GatewayPaymentStatus"/>.
/// </summary>
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

    /// <summary>An instruction built the old way names no customer, so existing callers keep their behaviour.</summary>
    [Fact]
    public void PaymentInstruction_BuiltWithoutTheCustomer_HasNone()
    {
        var instruction = new PaymentInstruction("order-1", 1000, "https://host.example/return", null, null);

        instruction.CustomerRef.Should().BeNull();
    }

    /// <summary>A plugin that honours the customer attaches the stored method to the customer named.</summary>
    [Fact]
    public async Task GetStatusAsync_CustomerGiven_AttachesTheMethodToThatCustomer()
    {
        var plugin = new SavingPaymentPlugin();
        var instruction = new PaymentInstruction(
            "order-3", 1000, "https://host.example/return", null, null, SavePaymentMethod: true, CustomerRef: "cus_1");

        var session = await plugin.CreatePaymentAsync(instruction, CancellationToken.None);
        await plugin.GetStatusAsync(session.GatewayOrderId, CancellationToken.None);

        plugin.MethodsOf("cus_1").Should().Equal(SavingPaymentPlugin.StoredMethod);
        plugin.MethodsOf("cus_2").Should().BeEmpty();
    }

    /// <summary>Reading the status of the same payment twice attaches the method to the customer only once.</summary>
    [Fact]
    public async Task GetStatusAsync_ReadTwice_AttachesTheMethodOnce()
    {
        var plugin = new SavingPaymentPlugin();
        var instruction = new PaymentInstruction(
            "order-4", 1000, "https://host.example/return", null, null, SavePaymentMethod: true, CustomerRef: "cus_1");

        var session = await plugin.CreatePaymentAsync(instruction, CancellationToken.None);
        await plugin.GetStatusAsync(session.GatewayOrderId, CancellationToken.None);
        await plugin.GetStatusAsync(session.GatewayOrderId, CancellationToken.None);

        plugin.MethodsOf("cus_1").Should().HaveCount(1);
    }

    /// <summary>A customer without the save flag attaches nothing: the customer alone never stores a method.</summary>
    [Fact]
    public async Task GetStatusAsync_CustomerGivenButNoFlag_AttachesNothing()
    {
        var plugin = new SavingPaymentPlugin();
        var instruction = new PaymentInstruction(
            "order-5", 1000, "https://host.example/return", null, null, CustomerRef: "cus_1");

        var session = await plugin.CreatePaymentAsync(instruction, CancellationToken.None);
        var status = await plugin.GetStatusAsync(session.GatewayOrderId, CancellationToken.None);

        status.SavedMethod.Should().BeNull();
        plugin.MethodsOf("cus_1").Should().BeEmpty();
    }

    /// <summary>A saving payment with no customer still reports the method and attaches it to nobody.</summary>
    [Fact]
    public async Task GetStatusAsync_FlagWithoutCustomer_ReportsTheMethodOnly()
    {
        var plugin = new SavingPaymentPlugin();
        var instruction = new PaymentInstruction(
            "order-6", 1000, "https://host.example/return", null, null, SavePaymentMethod: true);

        var session = await plugin.CreatePaymentAsync(instruction, CancellationToken.None);
        var status = await plugin.GetStatusAsync(session.GatewayOrderId, CancellationToken.None);

        status.SavedMethod.Should().Be(SavingPaymentPlugin.StoredMethod);
    }
}
