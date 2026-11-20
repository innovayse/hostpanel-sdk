namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>
/// The ISO 4217 table <see cref="PluginMoney"/> reads: every currency code with its numeric code and the number of
/// digits after the decimal point of its minor unit. A currency with no minor unit in the standard (precious metals,
/// funds codes, test codes) is not listed, so asking for one is refused instead of guessed.
/// </summary>
internal static class CurrencyMinorUnits
{
    /// <summary>The number of minor unit digits a currency has unless it is listed in one of the sets below.</summary>
    public const int DefaultDigits = 2;

    /// <summary>The numeric code of every currency, by its three letter code in upper case.</summary>
    public static readonly IReadOnlyDictionary<string, string> NumericByAlpha = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["AED"] = "784", ["AFN"] = "971", ["ALL"] = "008", ["AMD"] = "051", ["ANG"] = "532", ["AOA"] = "973",
        ["ARS"] = "032", ["AUD"] = "036", ["AWG"] = "533", ["AZN"] = "944", ["BAM"] = "977", ["BBD"] = "052",
        ["BDT"] = "050", ["BGN"] = "975", ["BHD"] = "048", ["BIF"] = "108", ["BMD"] = "060", ["BND"] = "096",
        ["BOB"] = "068", ["BRL"] = "986", ["BSD"] = "044", ["BTN"] = "064", ["BWP"] = "072", ["BYN"] = "933",
        ["BZD"] = "084", ["CAD"] = "124", ["CDF"] = "976", ["CHF"] = "756", ["CLF"] = "990", ["CLP"] = "152",
        ["CNY"] = "156", ["COP"] = "170", ["CRC"] = "188", ["CUP"] = "192", ["CVE"] = "132", ["CZK"] = "203",
        ["DJF"] = "262", ["DKK"] = "208", ["DOP"] = "214", ["DZD"] = "012", ["EGP"] = "818", ["ERN"] = "232",
        ["ETB"] = "230", ["EUR"] = "978", ["FJD"] = "242", ["FKP"] = "238", ["GBP"] = "826", ["GEL"] = "981",
        ["GHS"] = "936", ["GIP"] = "292", ["GMD"] = "270", ["GNF"] = "324", ["GTQ"] = "320", ["GYD"] = "328",
        ["HKD"] = "344", ["HNL"] = "340", ["HRK"] = "191", ["HTG"] = "332", ["HUF"] = "348", ["IDR"] = "360",
        ["ILS"] = "376", ["INR"] = "356", ["IQD"] = "368", ["IRR"] = "364", ["ISK"] = "352", ["JMD"] = "388",
        ["JOD"] = "400", ["JPY"] = "392", ["KES"] = "404", ["KGS"] = "417", ["KHR"] = "116", ["KMF"] = "174",
        ["KPW"] = "408", ["KRW"] = "410", ["KWD"] = "414", ["KYD"] = "136", ["KZT"] = "398", ["LAK"] = "418",
        ["LBP"] = "422", ["LKR"] = "144", ["LRD"] = "430", ["LSL"] = "426", ["LYD"] = "434", ["MAD"] = "504",
        ["MDL"] = "498", ["MGA"] = "969", ["MKD"] = "807", ["MMK"] = "104", ["MNT"] = "496", ["MOP"] = "446",
        ["MRU"] = "929", ["MUR"] = "480", ["MVR"] = "462", ["MWK"] = "454", ["MXN"] = "484", ["MYR"] = "458",
        ["MZN"] = "943", ["NAD"] = "516", ["NGN"] = "566", ["NIO"] = "558", ["NOK"] = "578", ["NPR"] = "524",
        ["NZD"] = "554", ["OMR"] = "512", ["PAB"] = "590", ["PEN"] = "604", ["PGK"] = "598", ["PHP"] = "608",
        ["PKR"] = "586", ["PLN"] = "985", ["PYG"] = "600", ["QAR"] = "634", ["RON"] = "946", ["RSD"] = "941",
        ["RUB"] = "643", ["RWF"] = "646", ["SAR"] = "682", ["SBD"] = "090", ["SCR"] = "690", ["SDG"] = "938",
        ["SEK"] = "752", ["SGD"] = "702", ["SHP"] = "654", ["SLE"] = "925", ["SOS"] = "706", ["SRD"] = "968",
        ["SSP"] = "728", ["STN"] = "930", ["SVC"] = "222", ["SYP"] = "760", ["SZL"] = "748", ["THB"] = "764",
        ["TJS"] = "972", ["TMT"] = "934", ["TND"] = "788", ["TOP"] = "776", ["TRY"] = "949", ["TTD"] = "780",
        ["TWD"] = "901", ["TZS"] = "834", ["UAH"] = "980", ["UGX"] = "800", ["USD"] = "840", ["UYI"] = "940",
        ["UYU"] = "858", ["UYW"] = "927", ["UZS"] = "860", ["VED"] = "926", ["VES"] = "928", ["VND"] = "704",
        ["VUV"] = "548", ["WST"] = "882", ["XAF"] = "950", ["XCD"] = "951", ["XOF"] = "952", ["XPF"] = "953",
        ["YER"] = "886", ["ZAR"] = "710", ["ZMW"] = "967", ["ZWG"] = "924", ["ZWL"] = "932",
    };

    /// <summary>The currencies that have no minor unit digits: the amount is a whole number of the main unit.</summary>
    public static readonly IReadOnlySet<string> NoDigits = new HashSet<string>(StringComparer.Ordinal)
    {
        "BIF", "CLP", "DJF", "GNF", "ISK", "JPY", "KMF", "KRW", "PYG", "RWF", "UGX", "UYI", "VND", "VUV", "XAF", "XOF",
        "XPF",
    };

    /// <summary>The currencies whose minor unit is a thousandth of the main unit.</summary>
    public static readonly IReadOnlySet<string> ThreeDigits = new HashSet<string>(StringComparer.Ordinal)
    {
        "BHD", "IQD", "JOD", "KWD", "LYD", "OMR", "TND",
    };

    /// <summary>The currencies whose minor unit is a ten thousandth of the main unit.</summary>
    public static readonly IReadOnlySet<string> FourDigits = new HashSet<string>(StringComparer.Ordinal)
    {
        "CLF", "UYW",
    };

    /// <summary>The three letter code of every currency, by its numeric code.</summary>
    public static readonly IReadOnlyDictionary<string, string> AlphaByNumeric =
        NumericByAlpha.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    /// <summary>Finds the number of minor unit digits of a listed currency, given its upper case three letter code.</summary>
    /// <param name="alpha">The three letter code, in upper case.</param>
    /// <returns>The digits, 0 to 4.</returns>
    public static int DigitsOf(string alpha)
    {
        if (NoDigits.Contains(alpha))
        {
            return 0;
        }

        if (ThreeDigits.Contains(alpha))
        {
            return 3;
        }

        return FourDigits.Contains(alpha) ? 4 : DefaultDigits;
    }
}
