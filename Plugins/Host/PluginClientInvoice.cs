using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.Host;

/// <summary>One invoice of the call's client. Gateway references and notes are not shared.</summary>
/// <param name="Id">The host's own id of this invoice, one of the caller's own records.</param>
/// <param name="Status">The state of the invoice.</param>
/// <param name="Total">The amount due in <paramref name="Currency"/>.</param>
/// <param name="Currency">The ISO 4217 alpha code of the invoice.</param>
/// <param name="InvoiceDate">When the invoice was issued.</param>
/// <param name="DueDate">When payment is due.</param>
/// <param name="PaidAt">When it was paid, or <see langword="null"/> when it is not paid.</param>
public sealed record PluginClientInvoice(
    int Id,
    PluginInvoiceStatus Status,
    decimal Total,
    string Currency,
    DateTimeOffset InvoiceDate,
    DateTimeOffset DueDate,
    DateTimeOffset? PaidAt);
