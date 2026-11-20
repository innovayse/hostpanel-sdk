namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// What kind of payment method a <see cref="StoredCredentials.SavedMethod"/> is. The values are appended and never
/// renumbered. <see cref="Card"/> is 0 so that a method built before kinds existed, and a host that does not fill the
/// kind, still read as a card.
/// </summary>
public enum SavedMethodKind
{
    /// <summary>A payment card. The default, so every method saved before kinds existed is a card.</summary>
    Card = 0,

    /// <summary>A bank account debited directly.</summary>
    BankAccount = 1,

    /// <summary>A direct debit mandate the payer signed.</summary>
    Mandate = 2,

    /// <summary>A wallet the payer holds with a payment provider.</summary>
    Wallet = 3,
}
