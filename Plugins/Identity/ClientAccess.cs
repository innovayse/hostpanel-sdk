namespace Innovayse.SDK.Plugins.Identity;

/// <summary>
/// What an identity plugin knows about the caller: which external account they act for and
/// what they may do on its behalf. The host maps the account to a client row (creating one on
/// the first order) — the plugin never touches persistence.
/// </summary>
/// <param name="Account">The external account the principal acts for.</param>
/// <param name="CanPurchase">Whether the principal may place orders and pay invoices for it.</param>
/// <param name="CanManagePaymentMethods">
/// Whether the principal may add, remove or default its saved payment methods.
/// </param>
public sealed record ClientAccess(ExternalAccount Account, bool CanPurchase, bool CanManagePaymentMethods);
