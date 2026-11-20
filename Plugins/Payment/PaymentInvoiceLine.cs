namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>
/// One line of the invoice a payment pays, for a gateway that prints or reports the lines (a fiscal receipt, a hosted
/// invoice). The line total is <see cref="Quantity"/> times <see cref="UnitAmountMinor"/>.
/// </summary>
/// <param name="Description">What the line is for, as the client sees it on the invoice.</param>
/// <param name="Quantity">How many units the line bills; may be fractional, for example hours.</param>
/// <param name="UnitAmountMinor">The price of one unit in minor currency units, in the currency of the payment.</param>
public sealed record PaymentInvoiceLine(string Description, decimal Quantity, long UnitAmountMinor);
