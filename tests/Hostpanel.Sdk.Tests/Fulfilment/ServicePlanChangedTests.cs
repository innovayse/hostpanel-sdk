using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Fulfilment;
using Hostpanel.Sdk.Plugins.Identity;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Fulfilment;

/// <summary>Pins the service plan change event of a fulfilment plugin: its context, its kinds and its default body.</summary>
public sealed class ServicePlanChangedTests
{
    /// <summary>The moment every sample change happened.</summary>
    private static readonly DateTimeOffset At = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);

    /// <summary>The kinds of change are pinned, and zero is never a kind.</summary>
    [Fact]
    public void ServicePlanChangeKind_Values_ArePinned()
    {
        var values = Enum.GetValues<ServicePlanChangeKind>();

        values.Select(value => (int)value).Should().Equal(1, 2, 3);
        values.Select(value => value.ToString()).Should().Equal("Upgrade", "Downgrade", "QuantityChange");
        Enum.IsDefined((ServicePlanChangeKind)0).Should().BeFalse();
    }

    /// <summary>A plugin built before 3.12.0 compiles without the member, and the default answers at once without doing anything.</summary>
    [Fact]
    public async Task Default_OnServicePlanChanged_DoesNothing()
    {
        IFulfilmentPlugin plugin = new QuietFulfilmentPlugin();

        var task = plugin.OnServicePlanChangedAsync(Sample("key-1"), CancellationToken.None);
        await task;

        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    /// <summary>The context keeps every value it was built with.</summary>
    [Fact]
    public void Context_KeepsItsValues()
    {
        var account = new ExternalAccount("crm", "acc-9", null, null);
        var previous = Line(1, 1);
        var current = Line(2, 3);

        var context = new ServicePlanChangedContext(7, 3, "ref-3", account, previous, current, ServicePlanChangeKind.QuantityChange, At, "key-1");

        context.ServiceId.Should().Be(7);
        context.ClientId.Should().Be(3);
        context.ClientRef.Should().Be("ref-3");
        context.Account.Should().Be(account);
        context.Previous.Should().Be(previous);
        context.Current.Should().Be(current);
        context.Kind.Should().Be(ServicePlanChangeKind.QuantityChange);
        context.ChangedAt.Should().Be(At);
        context.IdempotencyKey.Should().Be("key-1");
    }

    /// <summary>The printed form hides the client reference and shows the rest.</summary>
    [Fact]
    public void Context_ToString_MasksTheClientRef()
    {
        var text = Sample("key-1").ToString();

        text.Should().NotContain("client-ref-value");
        text.Should().Contain("key-1").And.Contain("Upgrade");
    }

    /// <summary>A plugin that overrides the member is called through the interface, and a repeated key is applied once.</summary>
    [Fact]
    public async Task Override_OnServicePlanChanged_IsCalledAndAppliesAKeyOnce()
    {
        var plugin = new PlanTrackingFulfilmentPlugin();
        IFulfilmentPlugin asInterface = plugin;

        await asInterface.OnServicePlanChangedAsync(Sample("key-1"), CancellationToken.None);
        await asInterface.OnServicePlanChangedAsync(Sample("key-1"), CancellationToken.None);
        await asInterface.OnServicePlanChangedAsync(Sample("key-2"), CancellationToken.None);

        plugin.Applied.Should().Be(2);
    }

    /// <summary>Builds a line of the given product and quantity.</summary>
    /// <param name="productId">The product id.</param>
    /// <param name="quantity">The units on the line.</param>
    /// <returns>The line.</returns>
    private static FulfilmentLine Line(int productId, int quantity)
    {
        return new FulfilmentLine(productId, "plan", null, quantity, 10m, "USD", PluginBillingCycle.Monthly);
    }

    /// <summary>Builds an upgrade context with the given key.</summary>
    /// <param name="key">The idempotency key.</param>
    /// <returns>The context.</returns>
    private static ServicePlanChangedContext Sample(string key)
    {
        return new ServicePlanChangedContext(7, 3, "client-ref-value", null, Line(1, 1), Line(2, 1), ServicePlanChangeKind.Upgrade, At, key);
    }
}
