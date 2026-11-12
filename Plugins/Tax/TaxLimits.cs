namespace Hostpanel.Sdk.Plugins.Tax;

/// <summary>
/// The limits of a <see cref="TaxCalculationRequest"/>. They are the host's: a host never sends more, and the contract
/// helper refuses a request that breaks one, so a plugin never has to cope with more than this.
/// </summary>
public static class TaxLimits
{
    /// <summary>The most lines in one request.</summary>
    public const int MaxLines = 500;

    /// <summary>The longest <see cref="TaxLine.LineId"/>, in characters.</summary>
    public const int MaxLineIdLength = 64;

    /// <summary>The longest tax number of a party, in characters.</summary>
    public const int MaxTaxIdLength = 64;

    /// <summary>The length of a currency code: three letters of ISO 4217.</summary>
    public const int CurrencyLength = 3;
}
