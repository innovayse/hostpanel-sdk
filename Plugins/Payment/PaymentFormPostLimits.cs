namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>
/// The limits the host keeps on a <see cref="PaymentFormPost"/>. A session that breaks one is refused as a failed
/// payment, so a plugin never has to cope with a partly rendered form.
/// </summary>
public static class PaymentFormPostLimits
{
    /// <summary>The most fields in one form.</summary>
    public const int MaxFields = 64;

    /// <summary>The longest field name, in characters.</summary>
    public const int MaxNameLength = 128;

    /// <summary>The longest field value, in characters.</summary>
    public const int MaxValueLength = 8192;

    /// <summary>The longest action URL, in characters.</summary>
    public const int MaxActionLength = 2048;
}
