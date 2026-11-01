namespace Innovayse.SDK.Plugins.StoredCredentials;

/// <summary>A payment method stored at the gateway. The host does not keep these; it lists them live.</summary>
/// <param name="MethodRef">The gateway-side method reference the host passes to a charge.</param>
/// <param name="Brand">Display brand of the method, for example the card network; null for a method that has none, such as a bank account.</param>
/// <param name="Last4">Last four digits (or characters) of the method, for display; null for a method that has none.</param>
/// <param name="ExpiryMonth">Expiry month (1 to 12); 0 when the method does not expire.</param>
/// <param name="ExpiryYear">Four digit expiry year; 0 when the method does not expire.</param>
/// <param name="IsDefault">Whether this is the customer's default method.</param>
public sealed record SavedMethod(string MethodRef, string? Brand, string? Last4, int ExpiryMonth, int ExpiryYear, bool IsDefault);
