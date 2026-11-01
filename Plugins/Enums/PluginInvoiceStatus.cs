namespace Innovayse.SDK.Plugins.Enums;

/// <summary>
/// The state of a client's invoice as <see cref="Host.IPluginHostApi"/> reports it. Values start at 1; 0 is not a
/// status.
/// </summary>
public enum PluginInvoiceStatus
{
    /// <summary>The invoice is saved but not issued yet.</summary>
    Draft = 1,

    /// <summary>The invoice is issued and payment is expected.</summary>
    Unpaid = 2,

    /// <summary>The invoice is paid in full.</summary>
    Paid = 3,

    /// <summary>The due date has passed without payment.</summary>
    Overdue = 4,

    /// <summary>The invoice is voided and will not be collected.</summary>
    Cancelled = 5,

    /// <summary>The payment is refunded in full.</summary>
    Refunded = 6,

    /// <summary>The invoice was passed to collections.</summary>
    Collections = 7,

    /// <summary>A payment was started and awaits confirmation.</summary>
    PaymentPending = 8,

    /// <summary>The invoice is paid and part of the money was refunded.</summary>
    PartiallyRefunded = 9,
}
