namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>
/// The limits the host keeps when it hands the parameters of a return request to
/// <see cref="IPaymentReturnPlugin.CompleteAsync"/>. A request that exceeds one is not passed on in part: the host
/// drops the whole set and the plugin sees none, so a plugin never has to cope with more than this.
/// </summary>
public static class PaymentReturnLimits
{
    /// <summary>The most parameters the plugin receives.</summary>
    public const int MaxParameters = 32;

    /// <summary>The longest parameter name, in characters.</summary>
    public const int MaxNameLength = 64;

    /// <summary>The longest parameter value, in characters.</summary>
    public const int MaxValueLength = 2048;
}
