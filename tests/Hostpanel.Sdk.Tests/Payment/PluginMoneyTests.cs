using System.Globalization;
using Hostpanel.Sdk.Plugins.Payment;

namespace Hostpanel.Sdk.Tests.Payment;

/// <summary>Tests of <see cref="PluginMoney"/>: the ISO 4217 minor unit digits and the conversions to and from decimal text.</summary>
public sealed class PluginMoneyTests
{
    /// <summary>
    /// The independent oracle: every listed currency as <c>CODE:numeric:digits</c>. It is written out here and not read
    /// from the table under test, so a wrong entry in the table fails a test.
    /// </summary>
    private const string Oracle =
        "AED:784:2 AFN:971:2 ALL:008:2 AMD:051:2 ANG:532:2 AOA:973:2 ARS:032:2 AUD:036:2 AWG:533:2 AZN:944:2 " +
        "BAM:977:2 BBD:052:2 BDT:050:2 BGN:975:2 BHD:048:3 BIF:108:0 BMD:060:2 BND:096:2 BOB:068:2 BRL:986:2 " +
        "BSD:044:2 BTN:064:2 BWP:072:2 BYN:933:2 BZD:084:2 CAD:124:2 CDF:976:2 CHF:756:2 CLF:990:4 CLP:152:0 " +
        "CNY:156:2 COP:170:2 CRC:188:2 CUP:192:2 CVE:132:2 CZK:203:2 DJF:262:0 DKK:208:2 DOP:214:2 DZD:012:2 " +
        "EGP:818:2 ERN:232:2 ETB:230:2 EUR:978:2 FJD:242:2 FKP:238:2 GBP:826:2 GEL:981:2 GHS:936:2 GIP:292:2 " +
        "GMD:270:2 GNF:324:0 GTQ:320:2 GYD:328:2 HKD:344:2 HNL:340:2 HRK:191:2 HTG:332:2 HUF:348:2 IDR:360:2 " +
        "ILS:376:2 INR:356:2 IQD:368:3 IRR:364:2 ISK:352:0 JMD:388:2 JOD:400:3 JPY:392:0 KES:404:2 KGS:417:2 " +
        "KHR:116:2 KMF:174:0 KPW:408:2 KRW:410:0 KWD:414:3 KYD:136:2 KZT:398:2 LAK:418:2 LBP:422:2 LKR:144:2 " +
        "LRD:430:2 LSL:426:2 LYD:434:3 MAD:504:2 MDL:498:2 MGA:969:2 MKD:807:2 MMK:104:2 MNT:496:2 MOP:446:2 " +
        "MRU:929:2 MUR:480:2 MVR:462:2 MWK:454:2 MXN:484:2 MYR:458:2 MZN:943:2 NAD:516:2 NGN:566:2 NIO:558:2 " +
        "NOK:578:2 NPR:524:2 NZD:554:2 OMR:512:3 PAB:590:2 PEN:604:2 PGK:598:2 PHP:608:2 PKR:586:2 PLN:985:2 " +
        "PYG:600:0 QAR:634:2 RON:946:2 RSD:941:2 RUB:643:2 RWF:646:0 SAR:682:2 SBD:090:2 SCR:690:2 SDG:938:2 " +
        "SEK:752:2 SGD:702:2 SHP:654:2 SLE:925:2 SOS:706:2 SRD:968:2 SSP:728:2 STN:930:2 SVC:222:2 SYP:760:2 " +
        "SZL:748:2 THB:764:2 TJS:972:2 TMT:934:2 TND:788:3 TOP:776:2 TRY:949:2 TTD:780:2 TWD:901:2 TZS:834:2 " +
        "UAH:980:2 UGX:800:0 USD:840:2 UYI:940:0 UYU:858:2 UYW:927:4 UZS:860:2 VED:926:2 VES:928:2 VND:704:0 " +
        "VUV:548:0 WST:882:2 XAF:950:0 XCD:951:2 XOF:952:0 XPF:953:0 YER:886:2 ZAR:710:2 ZMW:967:2 ZWG:924:2 " +
        "ZWL:932:2";

    /// <summary>Gets the oracle as rows of the code, the numeric code and the digits, for a theory.</summary>
    public static IEnumerable<object[]> Currencies()
    {
        foreach (var entry in Oracle.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split(':');
            yield return [parts[0], parts[1], int.Parse(parts[2], CultureInfo.InvariantCulture)];
        }
    }

    /// <summary>Every listed currency has its digits by the three letter code, in any case.</summary>
    [Theory]
    [MemberData(nameof(Currencies))]
    public void GetMinorUnitDigits_ByAlphaCode_AnyCase(string alpha, string numeric, int digits)
    {
        numeric.Should().HaveLength(3);
        PluginMoney.GetMinorUnitDigits(alpha).Should().Be(digits);
        PluginMoney.GetMinorUnitDigits(alpha.ToLowerInvariant()).Should().Be(digits);
    }

    /// <summary>Every listed currency has the same digits by its numeric code, the form the contract uses.</summary>
    [Theory]
    [MemberData(nameof(Currencies))]
    public void GetMinorUnitDigits_ByNumericCode(string alpha, string numeric, int digits)
    {
        alpha.Should().HaveLength(3);
        PluginMoney.GetMinorUnitDigits(numeric).Should().Be(digits);
    }

    /// <summary>The oracle has no repeated code or numeric code, and lists 17, 7 and 2 currencies with 0, 3 and 4 digits.</summary>
    [Fact]
    public void Oracle_IsConsistent()
    {
        var rows = Currencies().Select(row => (Alpha: (string)row[0], Numeric: (string)row[1], Digits: (int)row[2])).ToList();

        rows.Select(row => row.Alpha).Should().OnlyHaveUniqueItems();
        rows.Select(row => row.Numeric).Should().OnlyHaveUniqueItems();
        rows.Count(row => row.Digits == 0).Should().Be(17);
        rows.Count(row => row.Digits == 3).Should().Be(7);
        rows.Count(row => row.Digits == 4).Should().Be(2);
    }

    /// <summary>The table holds exactly the oracle's currencies: nothing is missing and nothing is extra.</summary>
    [Fact]
    public void Table_HoldsExactlyTheOracle()
    {
        var listed = new List<string>();
        for (var number = 0; number < 1000; number++)
        {
            var numeric = number.ToString("000", CultureInfo.InvariantCulture);
            if (PluginMoney.TryGetMinorUnitDigits(numeric, out _))
            {
                listed.Add(numeric);
            }
        }

        listed.Should().BeEquivalentTo(Currencies().Select(row => (string)row[1]));
    }

    /// <summary>A code that is not listed, in either form, is refused, and the digit count of a refused code is zero.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("XXX")]
    [InlineData("XAU")]
    [InlineData("999")]
    [InlineData("000")]
    [InlineData("84")]
    [InlineData(" USD")]
    [InlineData("U5D")]
    public void TryGetMinorUnitDigits_UnknownCode_ReturnsFalse(string code)
    {
        PluginMoney.TryGetMinorUnitDigits(code, out var digits).Should().BeFalse();
        digits.Should().Be(0);
    }

    /// <summary>A null code is refused by the try form without throwing, and by the throwing form with the right exception.</summary>
    [Fact]
    public void GetMinorUnitDigits_NullAndUnknown_Throw()
    {
        PluginMoney.TryGetMinorUnitDigits(null, out _).Should().BeFalse();
        var nullCode = () => PluginMoney.GetMinorUnitDigits(null!);
        var unknown = () => PluginMoney.GetMinorUnitDigits("XXX");

        nullCode.Should().Throw<ArgumentNullException>();
        unknown.Should().ThrowExactly<ArgumentException>();
    }

    /// <summary>Amounts are written with exactly the currency's fraction digits.</summary>
    [Theory]
    [InlineData(1234L, 2, "12.34")]
    [InlineData(1200L, 2, "12.00")]
    [InlineData(5L, 2, "0.05")]
    [InlineData(0L, 2, "0.00")]
    [InlineData(100000L, 2, "1000.00")]
    [InlineData(1200L, 0, "1200")]
    [InlineData(0L, 0, "0")]
    [InlineData(1234L, 3, "1.234")]
    [InlineData(7L, 3, "0.007")]
    [InlineData(1234567L, 4, "123.4567")]
    [InlineData(1L, 4, "0.0001")]
    [InlineData(-5L, 2, "-0.05")]
    [InlineData(-1234L, 2, "-12.34")]
    [InlineData(-1200L, 0, "-1200")]
    public void ToDecimalText_Digits_WritesTheText(long amountMinor, int digits, string expected)
    {
        PluginMoney.ToDecimalText(amountMinor, digits).Should().Be(expected);
    }

    /// <summary>The extreme values of a long are written without overflow.</summary>
    [Fact]
    public void ToDecimalText_LongExtremes_AreWritten()
    {
        PluginMoney.ToDecimalText(long.MaxValue, 2).Should().Be("92233720368547758.07");
        PluginMoney.ToDecimalText(long.MinValue, 2).Should().Be("-92233720368547758.08");
        PluginMoney.ToDecimalText(long.MinValue, 0).Should().Be("-9223372036854775808");
        PluginMoney.ToDecimalText(1L, PluginMoney.MaxDigits).Should().Be("0." + new string('0', PluginMoney.MaxDigits - 1) + "1");
    }

    /// <summary>The text does not depend on the culture of the process.</summary>
    [Fact]
    public void ToDecimalText_UnderCommaCulture_StillUsesTheDot()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");

            PluginMoney.ToDecimalText(123456789L, 2).Should().Be("1234567.89");
            PluginMoney.ToMinorUnits("1234567.89", 2).Should().Be(123456789L);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    /// <summary>The currency form takes the digits from the currency, by either code.</summary>
    [Theory]
    [InlineData(1234L, "USD", "12.34")]
    [InlineData(1234L, "840", "12.34")]
    [InlineData(1234L, "JPY", "1234")]
    [InlineData(1234L, "392", "1234")]
    [InlineData(1234L, "KWD", "1.234")]
    [InlineData(1234L, "CLF", "0.1234")]
    [InlineData(150000L, "051", "1500.00")]
    public void ToDecimalText_Currency_UsesItsDigits(long amountMinor, string currency, string expected)
    {
        PluginMoney.ToDecimalText(amountMinor, currency).Should().Be(expected);
    }

    /// <summary>A digit count outside 0 to 18 is refused in both directions.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(19)]
    [InlineData(int.MaxValue)]
    public void Conversions_DigitsOutOfRange_Throw(int digits)
    {
        var write = () => PluginMoney.ToDecimalText(1, digits);
        var read = () => PluginMoney.ToMinorUnits("1", digits);

        write.Should().Throw<ArgumentOutOfRangeException>();
        read.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>Null and unknown currency arguments are refused by the currency forms.</summary>
    [Fact]
    public void CurrencyForms_NullAndUnknown_Throw()
    {
        var nullCurrency = () => PluginMoney.ToDecimalText(1, null!);
        var unknownCurrency = () => PluginMoney.ToDecimalText(1, "XXX");
        var nullText = () => PluginMoney.ToMinorUnits(null!, 2);
        var nullTextCurrency = () => PluginMoney.ToMinorUnits(null!, "USD");
        var nullCurrencyRead = () => PluginMoney.ToMinorUnits("1", (string)null!);
        var unknownCurrencyRead = () => PluginMoney.ToMinorUnits("1", "XXX");

        nullCurrency.Should().Throw<ArgumentNullException>();
        unknownCurrency.Should().Throw<ArgumentException>();
        nullText.Should().Throw<ArgumentNullException>();
        nullTextCurrency.Should().Throw<ArgumentNullException>();
        nullCurrencyRead.Should().Throw<ArgumentNullException>();
        unknownCurrencyRead.Should().Throw<ArgumentException>();
    }

    /// <summary>Decimal text is read into minor units, with fewer fraction digits padded and extra zeros dropped.</summary>
    [Theory]
    [InlineData("12.34", 2, 1234L)]
    [InlineData("12.3", 2, 1230L)]
    [InlineData("12", 2, 1200L)]
    [InlineData("0.05", 2, 5L)]
    [InlineData("0", 2, 0L)]
    [InlineData("0.00", 2, 0L)]
    [InlineData("007.50", 2, 750L)]
    [InlineData("12.500", 2, 1250L)]
    [InlineData("12.0", 0, 12L)]
    [InlineData("1200", 0, 1200L)]
    [InlineData("1.234", 3, 1234L)]
    [InlineData("1.2", 3, 1200L)]
    [InlineData("123.4567", 4, 1234567L)]
    [InlineData("-0.05", 2, -5L)]
    [InlineData("-12.34", 2, -1234L)]
    [InlineData("-0", 2, 0L)]
    [InlineData("-12", 0, -12L)]
    public void ToMinorUnits_Text_ReadsTheAmount(string text, int digits, long expected)
    {
        PluginMoney.ToMinorUnits(text, digits).Should().Be(expected);
    }

    /// <summary>The currency form reads by either code.</summary>
    [Theory]
    [InlineData("12.34", "USD", 1234L)]
    [InlineData("12.34", "840", 1234L)]
    [InlineData("1234", "JPY", 1234L)]
    [InlineData("1.234", "kwd", 1234L)]
    public void ToMinorUnits_Currency_UsesItsDigits(string text, string currency, long expected)
    {
        PluginMoney.ToMinorUnits(text, currency).Should().Be(expected);
    }

    /// <summary>The extreme values of a long are read back.</summary>
    [Fact]
    public void ToMinorUnits_LongExtremes_AreRead()
    {
        PluginMoney.ToMinorUnits("92233720368547758.07", 2).Should().Be(long.MaxValue);
        PluginMoney.ToMinorUnits("-92233720368547758.08", 2).Should().Be(long.MinValue);
        PluginMoney.ToMinorUnits("-9223372036854775808", 0).Should().Be(long.MinValue);
    }

    /// <summary>An amount over the range of a long is an overflow, not a wrapped value.</summary>
    [Theory]
    [InlineData("92233720368547758.08", 2)]
    [InlineData("-92233720368547758.09", 2)]
    [InlineData("9223372036854775808", 0)]
    [InlineData("-9223372036854775809", 0)]
    [InlineData("99999999999999999999999999", 2)]
    public void ToMinorUnits_OutOfRange_ThrowsOverflow(string text, int digits)
    {
        var read = () => PluginMoney.ToMinorUnits(text, digits);

        read.Should().Throw<OverflowException>();
    }

    /// <summary>Anything that is not a plain decimal amount is a format error, and the message does not repeat the text.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("-")]
    [InlineData(".")]
    [InlineData(".5")]
    [InlineData("5.")]
    [InlineData("1.2.3")]
    [InlineData("+5")]
    [InlineData("--5")]
    [InlineData("- 5")]
    [InlineData(" 5")]
    [InlineData("5 ")]
    [InlineData("1,5")]
    [InlineData("1 000.00")]
    [InlineData("1e3")]
    [InlineData("0x10")]
    [InlineData("12.345")]
    [InlineData("12.3450001")]
    [InlineData("١٢")]
    [InlineData("NaN")]
    public void ToMinorUnits_NotAnAmount_ThrowsFormat(string text)
    {
        var read = () => PluginMoney.ToMinorUnits(text, 2);

        var thrown = read.Should().Throw<FormatException>().Which;
        if (text.Length > 1)
        {
            thrown.Message.Should().NotContain(text);
        }
    }

    /// <summary>An amount with a fraction is refused for a currency without digits, unless the fraction is only zeros.</summary>
    [Fact]
    public void ToMinorUnits_FractionForWholeCurrency_IsRefusedUnlessZero()
    {
        var read = () => PluginMoney.ToMinorUnits("12.5", "JPY");

        read.Should().Throw<FormatException>();
        PluginMoney.ToMinorUnits("12.00", "JPY").Should().Be(12L);
    }

    /// <summary>Writing and reading are inverse for a spread of amounts, in every digit count of the table.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void RoundTrip_AmountsSurvive(int digits)
    {
        long[] amounts = [0, 1, 9, 10, 99, 100, 101, 1234, 99999, 1000000, 123456789012, long.MaxValue, -1, -100, -123456, long.MinValue];

        foreach (var amount in amounts)
        {
            PluginMoney.ToMinorUnits(PluginMoney.ToDecimalText(amount, digits), digits).Should().Be(amount);
        }
    }
}
