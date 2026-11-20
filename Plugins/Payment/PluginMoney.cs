using System.Globalization;

namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>
/// The money arithmetic every payment plugin repeats: how many digits the minor unit of a currency has (ISO 4217) and
/// how an amount in minor units becomes the decimal text a gateway expects and back. The host and the contract speak
/// minor units (<see cref="PaymentInstruction"/>, <see cref="RefundInstruction"/>); a gateway often wants "12.34" or
/// "1200" instead. The conversions use whole numbers only, never a floating point type, so no amount is rounded, and
/// the text is always written with a dot and no group separator whatever the culture of the process. A currency is
/// named by its ISO 4217 numeric code, the form <see cref="IPaymentPlugin.CurrencyCode"/> uses, or by its three letter
/// code, in any case.
/// </summary>
public static class PluginMoney
{
    /// <summary>The most fraction digits a conversion accepts; a <see cref="long"/> holds at most 19 digits.</summary>
    public const int MaxDigits = 18;

    /// <summary>The text of a failed parse: it names the rule, never the value the gateway sent.</summary>
    private const string NotAnAmountMessage =
        "The text is not a plain decimal amount: an optional minus sign, digits, and an optional dot with digits, " +
        "with no more fraction digits than the currency has unless the extra ones are zero.";

    /// <summary>The text of an amount that does not fit a <see cref="long"/>.</summary>
    private const string TooLargeMessage = "The amount is too large to hold in minor currency units.";

    /// <summary>Finds the number of minor unit digits of a currency.</summary>
    /// <param name="currency">The ISO 4217 numeric code (for example "840") or the three letter code (for example "usd").</param>
    /// <param name="digits">The digits after the decimal point of the minor unit, 0 to 4; zero when the currency is not known.</param>
    /// <returns><see langword="true"/> when the currency is a listed ISO 4217 currency.</returns>
    public static bool TryGetMinorUnitDigits(string? currency, out int digits)
    {
        digits = 0;
        if (currency is null || currency.Length != 3)
        {
            return false;
        }

        var alpha = currency.ToUpperInvariant();
        if (!CurrencyMinorUnits.NumericByAlpha.ContainsKey(alpha) && !CurrencyMinorUnits.AlphaByNumeric.TryGetValue(currency, out alpha))
        {
            return false;
        }

        digits = CurrencyMinorUnits.DigitsOf(alpha);
        return true;
    }

    /// <summary>Gets the number of minor unit digits of a currency.</summary>
    /// <param name="currency">The ISO 4217 numeric code (for example "051") or the three letter code (for example "AMD").</param>
    /// <returns>The digits after the decimal point of the minor unit: 0 for yen, 2 for dollars, 3 for dinars, 4 for a few units of account.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="currency"/> is not a listed ISO 4217 currency.</exception>
    public static int GetMinorUnitDigits(string currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        if (!TryGetMinorUnitDigits(currency, out var digits))
        {
            throw new ArgumentException("The currency is not a known ISO 4217 code.", nameof(currency));
        }

        return digits;
    }

    /// <summary>Writes an amount in minor units as the decimal text of the currency, such as "12.34" or "1200".</summary>
    /// <param name="amountMinor">The amount in minor currency units; may be negative.</param>
    /// <param name="currency">The ISO 4217 numeric code or the three letter code.</param>
    /// <returns>The amount with exactly as many fraction digits as the currency has and no fraction part when it has none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="currency"/> is not a listed ISO 4217 currency.</exception>
    public static string ToDecimalText(long amountMinor, string currency)
    {
        return ToDecimalText(amountMinor, GetMinorUnitDigits(currency));
    }

    /// <summary>Writes an amount in minor units as decimal text with a fixed number of fraction digits.</summary>
    /// <param name="amountMinor">The amount in minor currency units; may be negative.</param>
    /// <param name="digits">The fraction digits, 0 to <see cref="MaxDigits"/>.</param>
    /// <returns>The amount, for example 1234 with 2 digits is "12.34", 5 with 2 is "0.05", -5 with 2 is "-0.05", 1200 with 0 is "1200".</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="digits"/> is negative or more than <see cref="MaxDigits"/>.</exception>
    public static string ToDecimalText(long amountMinor, int digits)
    {
        ValidateDigits(digits);

        // The magnitude is taken as an unsigned number, so long.MinValue does not overflow on negation.
        var magnitude = amountMinor < 0 ? (ulong)(-(amountMinor + 1)) + 1UL : (ulong)amountMinor;
        var sign = amountMinor < 0 ? "-" : string.Empty;
        var text = magnitude.ToString(CultureInfo.InvariantCulture);
        if (digits == 0)
        {
            return sign + text;
        }

        text = text.PadLeft(digits + 1, '0');
        var split = text.Length - digits;
        return string.Concat(sign, text.AsSpan(0, split), ".", text.AsSpan(split));
    }

    /// <summary>Reads the decimal text of an amount, as a gateway writes it, into minor units.</summary>
    /// <param name="decimalText">An optional minus sign, digits, and optionally a dot followed by digits, such as "12.34", "12.3", "12" or "-0.05".</param>
    /// <param name="currency">The ISO 4217 numeric code or the three letter code.</param>
    /// <returns>The amount in minor currency units.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="decimalText"/> or <paramref name="currency"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="currency"/> is not a listed ISO 4217 currency.</exception>
    /// <exception cref="FormatException">The text is not a plain decimal amount, or has a non-zero digit beyond the currency's minor unit.</exception>
    /// <exception cref="OverflowException">The amount does not fit a <see cref="long"/>.</exception>
    public static long ToMinorUnits(string decimalText, string currency)
    {
        return ToMinorUnits(decimalText, GetMinorUnitDigits(currency));
    }

    /// <summary>Reads the decimal text of an amount into minor units, for a currency with a given number of fraction digits.</summary>
    /// <param name="decimalText">
    /// An optional minus sign, digits, and optionally a dot followed by digits. Fewer fraction digits than
    /// <paramref name="digits"/> are allowed ("12.3" is 1230 with 2 digits). More are allowed only when the extra ones
    /// are all zero ("12.500" is 1250 with 2 digits), because anything else would lose part of the amount. No white
    /// space, plus sign, group separator or exponent is accepted, and neither is a text that starts or ends with the dot.
    /// </param>
    /// <param name="digits">The fraction digits of the currency, 0 to <see cref="MaxDigits"/>.</param>
    /// <returns>The amount in minor currency units.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="decimalText"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="digits"/> is negative or more than <see cref="MaxDigits"/>.</exception>
    /// <exception cref="FormatException">The text is not a plain decimal amount, or has a non-zero digit beyond <paramref name="digits"/>.</exception>
    /// <exception cref="OverflowException">The amount does not fit a <see cref="long"/>.</exception>
    public static long ToMinorUnits(string decimalText, int digits)
    {
        ArgumentNullException.ThrowIfNull(decimalText);
        ValidateDigits(digits);

        var negative = decimalText.StartsWith('-');
        var unsigned = negative ? decimalText.AsSpan(1) : decimalText.AsSpan();
        var dot = unsigned.IndexOf('.');
        var whole = dot < 0 ? unsigned : unsigned[..dot];
        var fraction = dot < 0 ? ReadOnlySpan<char>.Empty : unsigned[(dot + 1)..];
        if (!IsDigits(whole) || (dot >= 0 && !IsDigits(fraction)))
        {
            throw new FormatException(NotAnAmountMessage);
        }

        if (fraction.Length > digits && fraction[digits..].IndexOfAnyExcept('0') >= 0)
        {
            throw new FormatException(NotAnAmountMessage);
        }

        var kept = fraction.Length > digits ? fraction[..digits] : fraction;
        var combined = string.Concat(whole, kept, new string('0', digits - kept.Length));
        if (!ulong.TryParse(combined, NumberStyles.None, CultureInfo.InvariantCulture, out var magnitude))
        {
            throw new OverflowException(TooLargeMessage);
        }

        return ApplySign(magnitude, negative);
    }

    /// <summary>Checks the fraction digit count of a conversion.</summary>
    /// <param name="digits">The count to check.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="digits"/> is negative or more than <see cref="MaxDigits"/>.</exception>
    private static void ValidateDigits(int digits)
    {
        if (digits < 0 || digits > MaxDigits)
        {
            throw new ArgumentOutOfRangeException(nameof(digits), "The fraction digits must be from 0 to 18.");
        }
    }

    /// <summary>Tells whether a text is one or more ASCII digits.</summary>
    /// <param name="text">The text to check.</param>
    /// <returns><see langword="true"/> when it is not empty and holds only the digits 0 to 9.</returns>
    private static bool IsDigits(ReadOnlySpan<char> text)
    {
        return !text.IsEmpty && !text.ContainsAnyExceptInRange('0', '9');
    }

    /// <summary>Turns a magnitude and a sign into a <see cref="long"/>, refusing what does not fit.</summary>
    /// <param name="magnitude">The absolute value.</param>
    /// <param name="negative">Whether the amount is below zero.</param>
    /// <returns>The signed amount.</returns>
    /// <exception cref="OverflowException">The amount does not fit a <see cref="long"/>.</exception>
    private static long ApplySign(ulong magnitude, bool negative)
    {
        if (!negative)
        {
            return magnitude <= long.MaxValue ? (long)magnitude : throw new OverflowException(TooLargeMessage);
        }

        // long.MinValue has one more unit of magnitude than long.MaxValue, so it is the one value that is not a negation.
        if (magnitude == (ulong)long.MaxValue + 1UL)
        {
            return long.MinValue;
        }

        return magnitude <= long.MaxValue ? -(long)magnitude : throw new OverflowException(TooLargeMessage);
    }
}
