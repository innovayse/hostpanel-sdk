using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.StoredCredentials;

/// <summary>A payment method stored at the gateway. The host does not keep these; it lists them live.</summary>
/// <param name="MethodRef">The gateway-side method reference the host passes to a charge.</param>
/// <param name="Brand">Display brand of the method, for example the card network; null for a method that has none, such as a bank account.</param>
/// <param name="Last4">Last four digits (or characters) of the method, for display; null for a method that has none.</param>
/// <param name="ExpiryMonth">Expiry month (1 to 12); 0 when the method does not expire. Together with <paramref name="ExpiryYear"/> this is the optional expiry, see <see cref="HasExpiry"/>.</param>
/// <param name="ExpiryYear">Four digit expiry year; 0 when the method does not expire.</param>
/// <param name="IsDefault">Whether this is the customer's default method.</param>
public sealed record SavedMethod(string MethodRef, string? Brand, string? Last4, int ExpiryMonth, int ExpiryYear, bool IsDefault)
{
    /// <summary>
    /// Gets what kind of method this is. <see cref="SavedMethodKind.Card"/> by default, so a method built the old way is a
    /// card and an existing card plugin changes nothing. A plugin that stores a bank account, a mandate or a wallet sets it
    /// with an object initializer, and leaves the expiry at 0 for a kind that has none. Since 3.11.0; a host built on an
    /// earlier version ignores it and shows every method as a card.
    /// </summary>
    public SavedMethodKind Kind { get; init; } = SavedMethodKind.Card;

    /// <summary>Gets whether the method expires: both the month and the year are set. A bank account or a mandate usually does not.</summary>
    public bool HasExpiry => ExpiryMonth > 0 && ExpiryYear > 0;
}
