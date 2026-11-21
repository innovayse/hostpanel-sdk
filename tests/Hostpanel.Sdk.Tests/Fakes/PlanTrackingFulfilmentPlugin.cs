using Hostpanel.Sdk.Plugins.Fulfilment;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>A fulfilment plugin that overrides <see cref="IFulfilmentPlugin.OnServicePlanChangedAsync"/> and applies each key once.</summary>
internal sealed class PlanTrackingFulfilmentPlugin : IFulfilmentPlugin
{
    /// <summary>The keys already applied.</summary>
    private readonly HashSet<string> _applied = [];

    /// <summary>Gets the number of changes applied, a repeated key counted once.</summary>
    public int Applied => _applied.Count;

    /// <inheritdoc />
    public Task OnInvoicePaidAsync(InvoicePaidContext context, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<RefundAdvice> AdviseRefundAsync(RefundAdviceContext context, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task OnInvoiceRefundedAsync(InvoiceRefundedContext context, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnSubscriptionLapsedAsync(SubscriptionLapsedContext context, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnServicePlanChangedAsync(ServicePlanChangedContext context, CancellationToken ct)
    {
        _applied.Add(context.IdempotencyKey);
        return Task.CompletedTask;
    }
}
