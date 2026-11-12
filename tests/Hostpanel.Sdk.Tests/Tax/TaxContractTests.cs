using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.StoredCredentials;
using Hostpanel.Sdk.Plugins.Tax;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Tax;

/// <summary>
/// Tests of <see cref="TaxContract"/>: it accepts a well behaved plugin and rejects each way of breaking a promise.
/// </summary>
public sealed class TaxContractTests
{
    /// <summary>A plugin that calculates every line exactly passes, and the helper calls it once.</summary>
    [Fact]
    public async Task AssertCalculation_ExactLines_Passes()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => TaxFixtures.Correct(request));

        var result = await TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        result.Outcome.Should().Be(TaxOutcome.Calculated);
        result.Total.Should().Be(30.00m);
        plugin.Calls.Should().Be(1);
    }

    /// <summary>
    /// With prices that include tax the tax is taken out of the line total, and the exact amounts pass.
    /// </summary>
    [Fact]
    public async Task AssertCalculation_InclusivePrices_Passes()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => TaxFixtures.Correct(request));

        var result = await TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.InclusiveRequest);

        result.Lines!.Select(line => line.Amount).Should().Equal(16.67m, 8.33m);
    }

    /// <summary>A rate of 0 with an amount of 0 is a real answer, for example a reverse charged supply.</summary>
    [Fact]
    public async Task AssertCalculation_ZeroRateAndAmount_Passes()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => TaxCalculationResult.Calculated(
            request.Lines.Select(line => new TaxLineResult(line.LineId, 0m, 0m, TaxFixtures.France)).ToList()));

        var result = await TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        result.Total.Should().Be(0m);
    }

    /// <summary>An invoice without lines is answered with no lines.</summary>
    [Fact]
    public async Task AssertCalculation_NoLines_Passes()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => TaxCalculationResult.Calculated([]));

        var request = TaxFixtures.Request with { Lines = [] };

        var result = await TaxContract.AssertCalculationIsWellFormedAsync(plugin, request);

        result.Lines.Should().BeEmpty();
    }

    /// <summary>A rounding difference inside the tolerance passes and one outside it is rejected.</summary>
    [Fact]
    public async Task AssertCalculation_ToleranceDecidesRounding()
    {
        var plugin = new ScriptedTaxPlugin((asked, _) => Change(
            asked, line => line with { Amount = Math.Round(line.Amount, 0) }));
        var request = TaxFixtures.Request with { Lines = [new TaxLine("l1", 12.34m, 1, null, false)] };

        var strict = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, request);
        var whole = await TaxContract.AssertCalculationIsWellFormedAsync(plugin, request, tolerance: 0.5m);

        await AssertViolationAsync(strict, "does not match its rate");
        whole.Outcome.Should().Be(TaxOutcome.Calculated);
    }

    /// <summary>A plugin that throws is rejected, with the exception type and not its message.</summary>
    [Fact]
    public async Task AssertCalculation_Throws_Throws()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => throw new InvalidOperationException("secret detail"));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain("InvalidOperationException").And.NotContain("secret detail");
    }

    /// <summary>A plugin that answers null is rejected.</summary>
    [Fact]
    public async Task AssertCalculation_AnswersNull_Throws()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => null);

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "null");
    }

    /// <summary>A plugin that refuses where a calculation was arranged is rejected.</summary>
    [Fact]
    public async Task AssertCalculation_Refuses_Throws()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => TaxCalculationResult.Refused(TaxRefusalReason.AddressUnsupported));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "Refused");
    }

    /// <summary>An outcome that is not defined is rejected.</summary>
    [Fact]
    public async Task AssertCalculation_UndefinedOutcome_Throws()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => new TaxCalculationResult(default, null, null));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "not a TaxOutcome");
    }

    /// <summary>A calculation without lines is rejected: an empty invoice is an empty list.</summary>
    [Fact]
    public async Task AssertCalculation_CalculatedWithoutLines_Throws()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => new TaxCalculationResult(TaxOutcome.Calculated, null, null));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "without Lines");
    }

    /// <summary>A calculation that also carries a reason is rejected.</summary>
    [Fact]
    public async Task AssertCalculation_CalculatedWithReason_Throws()
    {
        var plugin = new ScriptedTaxPlugin(
            (request, _) => TaxFixtures.Correct(request) with { Reason = TaxRefusalReason.Other });

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "only a refusal has one");
    }

    /// <summary>A missing line is rejected, naming it.</summary>
    [Fact]
    public async Task AssertCalculation_MissingLine_Throws()
    {
        var plugin = new ScriptedTaxPlugin(
            (request, _) => TaxCalculationResult.Calculated(TaxFixtures.Correct(request).Lines!.Take(1).ToList()));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "did not answer line 'l2'");
    }

    /// <summary>A line that was not requested is rejected.</summary>
    [Fact]
    public async Task AssertCalculation_UnknownLine_Throws()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => TaxCalculationResult.Calculated(
            [.. TaxFixtures.Correct(request).Lines!, new TaxLineResult("l9", 0m, 0m, TaxFixtures.France)]));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "'l9'", "not requested");
    }

    /// <summary>A line answered twice is rejected.</summary>
    [Fact]
    public async Task AssertCalculation_DuplicateLine_Throws()
    {
        var plugin = new ScriptedTaxPlugin((request, _) =>
        {
            var lines = TaxFixtures.Correct(request).Lines!;
            return TaxCalculationResult.Calculated([lines[0], lines[0], lines[1]]);
        });

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "twice");
    }

    /// <summary>A negative amount is rejected.</summary>
    [Fact]
    public async Task AssertCalculation_NegativeAmount_Throws()
    {
        var plugin = new ScriptedTaxPlugin(
            (request, _) => Change(request, line => line with { Amount = -line.Amount }));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "negative amount");
    }

    /// <summary>A rate outside 0 to 100 is rejected.</summary>
    [Fact]
    public async Task AssertCalculation_RateOutOfRange_Throws()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => Change(request, line => line with { Rate = 120m }));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "rate outside 0 to 100");
    }

    /// <summary>An amount that does not follow the rate is rejected.</summary>
    [Fact]
    public async Task AssertCalculation_AmountDoesNotMatchRate_Throws()
    {
        var plugin = new ScriptedTaxPlugin(
            (request, _) => Change(request, line => line with { Amount = line.Amount + 1m }));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "does not match its rate");
    }

    /// <summary>
    /// An exclusive amount sent for inclusive prices does not match, so the price mode is part of the check.
    /// </summary>
    [Fact]
    public async Task AssertCalculation_ExclusiveAmountForInclusivePrices_Throws()
    {
        var plugin = new ScriptedTaxPlugin(
            (request, _) => TaxFixtures.Correct(request with { PricesIncludeTax = false }));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.InclusiveRequest);

        await AssertViolationAsync(run, "prices include tax");
    }

    /// <summary>A jurisdiction without a country code is rejected.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("fr")]
    [InlineData("FRA")]
    public async Task AssertCalculation_BadCountry_Throws(string country)
    {
        var plugin = new ScriptedTaxPlugin(
            (request, _) => Change(request, line => line with { Jurisdiction = new TaxJurisdiction(country, null) }));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "jurisdiction country");
    }

    /// <summary>A refusal without a reason is rejected.</summary>
    [Fact]
    public async Task AssertUncalculable_RefusalWithoutReason_Throws()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => new TaxCalculationResult(TaxOutcome.Refused, null, null));

        var run = () => TaxContract.AssertUncalculableRequestIsRefusedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "without a TaxRefusalReason");
    }

    /// <summary>A refusal with a reason passes.</summary>
    [Fact]
    public async Task AssertUncalculable_Refusal_Passes()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => TaxCalculationResult.Refused(TaxRefusalReason.AddressUnsupported));

        var result = await TaxContract.AssertUncalculableRequestIsRefusedAsync(plugin, TaxFixtures.Request);

        result.Reason.Should().Be(TaxRefusalReason.AddressUnsupported);
    }

    /// <summary>A plugin that guesses a rate for an address it cannot calculate is rejected.</summary>
    [Fact]
    public async Task AssertUncalculable_GuessedCalculation_Throws()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => TaxFixtures.Correct(request));

        var run = () => TaxContract.AssertUncalculableRequestIsRefusedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "never guessed");
    }

    /// <summary>A plugin that throws instead of refusing is rejected.</summary>
    [Fact]
    public async Task AssertUncalculable_Throws_Throws()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => throw new InvalidOperationException());

        var run = () => TaxContract.AssertUncalculableRequestIsRefusedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "threw");
    }

    /// <summary>A plugin that answers unavailable for an unreachable service passes.</summary>
    [Fact]
    public async Task AssertUnreachable_Unavailable_Passes()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => TaxCalculationResult.Unavailable());

        var result = await TaxContract.AssertUnreachableServiceIsUnavailableAsync(plugin, TaxFixtures.Request);

        result.Outcome.Should().Be(TaxOutcome.Unavailable);
        result.Total.Should().BeNull();
    }

    /// <summary>A plugin that turns a failed service into zero tax is rejected.</summary>
    [Fact]
    public async Task AssertUnreachable_ZeroTax_Throws()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => TaxCalculationResult.Calculated(
            request.Lines.Select(line => new TaxLineResult(line.LineId, 0m, 0m, TaxFixtures.France)).ToList()));

        var run = () => TaxContract.AssertUnreachableServiceIsUnavailableAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "never zero tax");
    }

    /// <summary>A plugin that presents an unreachable service as a refusal of the request is rejected.</summary>
    [Fact]
    public async Task AssertUnreachable_Refusal_Throws()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => TaxCalculationResult.Refused(TaxRefusalReason.Other));

        var run = () => TaxContract.AssertUnreachableServiceIsUnavailableAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "Refused");
    }

    /// <summary>An unavailable answer that carries a reason or lines is rejected.</summary>
    [Fact]
    public async Task AssertUnreachable_UnavailableWithReason_Throws()
    {
        var plugin = new ScriptedTaxPlugin(
            (_, _) => new TaxCalculationResult(TaxOutcome.Unavailable, null, TaxRefusalReason.Other));

        var run = () => TaxContract.AssertUnreachableServiceIsUnavailableAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "only a refusal has one");
    }

    /// <summary>
    /// A plugin that answers the same lines on every call passes, and the helper calls it as often as asked.
    /// </summary>
    [Fact]
    public async Task AssertRepeatable_SameLines_Passes()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => TaxFixtures.Correct(request));

        var first = await TaxContract.AssertCalculationIsRepeatableAsync(plugin, TaxFixtures.Request, calls: 4);

        first.Outcome.Should().Be(TaxOutcome.Calculated);
        plugin.Calls.Should().Be(4);
    }

    /// <summary>The same lines in another order are the same calculation.</summary>
    [Fact]
    public async Task AssertRepeatable_LinesInAnotherOrder_Passes()
    {
        var plugin = new ScriptedTaxPlugin((request, call) =>
        {
            var lines = TaxFixtures.Correct(request).Lines!;
            return TaxCalculationResult.Calculated(call == 1 ? lines : lines.Reverse().ToList());
        });

        var run = () => TaxContract.AssertCalculationIsRepeatableAsync(plugin, TaxFixtures.Request);

        await run.Should().NotThrowAsync();
    }

    /// <summary>A plugin whose repeat answers another amount is rejected, naming the call.</summary>
    [Fact]
    public async Task AssertRepeatable_DifferentOnRepeat_Throws()
    {
        var plugin = new ScriptedTaxPlugin((request, call) => call == 1
            ? TaxFixtures.Correct(request)
            : Change(request, line => line with { Rate = 21m, Amount = Math.Round(line.Amount / 20m * 21m, 2) }));

        var run = () => TaxContract.AssertCalculationIsRepeatableAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "call 2");
    }

    /// <summary>A plugin that fails the repeat is rejected.</summary>
    [Fact]
    public async Task AssertRepeatable_ThrowsOnRepeat_Throws()
    {
        var plugin = new ScriptedTaxPlugin(
            (request, call) => call == 1 ? TaxFixtures.Correct(request) : throw new TimeoutException());

        var run = () => TaxContract.AssertCalculationIsRepeatableAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "call 2", "TimeoutException");
    }

    /// <summary>Calling with fewer than two calls is a mistake of the caller.</summary>
    [Fact]
    public async Task AssertRepeatable_OneCall_Throws()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => TaxFixtures.Correct(request));

        var run = () => TaxContract.AssertCalculationIsRepeatableAsync(plugin, TaxFixtures.Request, calls: 1);

        await run.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>A negative tolerance is a mistake of the caller.</summary>
    [Fact]
    public async Task AssertCalculation_NegativeTolerance_Throws()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => TaxFixtures.Correct(request));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request, tolerance: -0.01m);

        await run.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>A null plugin or request is a mistake of the caller, thrown when the helper is called.</summary>
    [Fact]
    public async Task Assert_NullArguments_Throw()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => TaxFixtures.Correct(request));

        var nullPlugin = () => TaxContract.AssertCalculationIsWellFormedAsync(null!, TaxFixtures.Request);
        var nullRequest = () => TaxContract.AssertUncalculableRequestIsRefusedAsync(plugin, null!);

        await nullPlugin.Should().ThrowAsync<ArgumentNullException>();
        await nullRequest.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// A malformed request is a mistake of the caller: no id, a negative amount, a quantity below 1, a repeated id or
    /// no currency.
    /// </summary>
    [Fact]
    public async Task Assert_MalformedRequest_Throws()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => TaxFixtures.Correct(request));
        var line = new TaxLine("l1", 10m, 1, null, false);

        var requests = new[]
        {
            TaxFixtures.Request with { Currency = " " },
            TaxFixtures.Request with { Lines = [line with { LineId = " " }] },
            TaxFixtures.Request with { Lines = [line with { Amount = -1m }] },
            TaxFixtures.Request with { Lines = [line with { Quantity = 0 }] },
            TaxFixtures.Request with { Lines = [line, line] },
            TaxFixtures.Request with { Currency = "eur" },
            TaxFixtures.Request with { Currency = "EURO" },
            TaxFixtures.Request with { Seller = TaxFixtures.Request.Seller with { Address = null! } },
            TaxFixtures.Request with { Buyer = TaxFixtures.Request.Buyer with { Address = null! } },
            TaxFixtures.Request with { Buyer = TaxFixtures.Request.Buyer with { TaxId = new string('1', 65) } },
            TaxFixtures.Request with { Lines = [line with { LineId = new string('a', 65) }] },
            TaxFixtures.Request with
            {
                Lines = Enumerable.Range(0, TaxLimits.MaxLines + 1)
                    .Select(index => line with { LineId = "l" + index }).ToList(),
            },
        };

        foreach (var request in requests)
        {
            var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, request);

            await run.Should().ThrowAsync<ArgumentException>();
        }

        plugin.Calls.Should().Be(0);
    }

    /// <summary>A refusal that also carries lines is rejected.</summary>
    [Fact]
    public async Task AssertUncalculable_RefusedWithLines_Throws()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => new TaxCalculationResult(
            TaxOutcome.Refused, TaxFixtures.Correct(request).Lines, TaxRefusalReason.Other));

        var run = () => TaxContract.AssertUncalculableRequestIsRefusedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "with Lines");
    }

    /// <summary>An unavailable answer that carries lines is rejected.</summary>
    [Fact]
    public async Task AssertUnreachable_UnavailableWithLines_Throws()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => new TaxCalculationResult(
            TaxOutcome.Unavailable, TaxFixtures.Correct(request).Lines, null));

        var run = () => TaxContract.AssertUnreachableServiceIsUnavailableAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "with Lines");
    }

    /// <summary>A reason that is not a defined value is rejected.</summary>
    [Fact]
    public async Task AssertUncalculable_UndefinedReason_Throws()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => new TaxCalculationResult(
            TaxOutcome.Refused, null, (TaxRefusalReason)99));

        var run = () => TaxContract.AssertUncalculableRequestIsRefusedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "not a TaxRefusalReason");
    }

    /// <summary>A result line without an id is rejected.</summary>
    [Fact]
    public async Task AssertCalculation_LineWithoutId_Throws()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => Change(request, line => line with { LineId = " " }));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "without a LineId");
    }

    /// <summary>A null entry among the result lines is rejected.</summary>
    [Fact]
    public async Task AssertCalculation_NullLine_Throws()
    {
        var plugin = new ScriptedTaxPlugin((_, _) => new TaxCalculationResult(
            TaxOutcome.Calculated, new TaxLineResult[] { null! }, null));

        var run = () => TaxContract.AssertCalculationIsWellFormedAsync(plugin, TaxFixtures.Request);

        await AssertViolationAsync(run, "without a LineId");
    }

    /// <summary>Every helper reports a mistake of the caller when it is called, not from the returned task.</summary>
    [Fact]
    public void Assert_ArgumentErrors_AreThrownWhenCalled()
    {
        var plugin = new ScriptedTaxPlugin((request, _) => TaxFixtures.Correct(request));

        Action wellFormed = () => _ = TaxContract.AssertCalculationIsWellFormedAsync(plugin, null!);
        Action repeatable = () => _ = TaxContract.AssertCalculationIsRepeatableAsync(plugin, null!);
        Action refused = () => _ = TaxContract.AssertUncalculableRequestIsRefusedAsync(plugin, null!);
        Action unavailable = () => _ = TaxContract.AssertUnreachableServiceIsUnavailableAsync(plugin, null!);

        wellFormed.Should().Throw<ArgumentNullException>();
        repeatable.Should().Throw<ArgumentNullException>();
        refused.Should().Throw<ArgumentNullException>();
        unavailable.Should().Throw<ArgumentNullException>();
    }

    /// <summary>A printed form that shows a tax number or a street is rejected, without repeating it.</summary>
    [Fact]
    public void AssertHidesSecrets_Leak_Throws()
    {
        var run = () => TaxContract.AssertHidesSecrets("tax id " + TaxFixtures.BuyerTaxId, TaxFixtures.BuyerTaxId);

        var message = run.Should().Throw<PluginContractViolationException>().Which.Message;
        message.Should().NotContain(TaxFixtures.BuyerTaxId).And.Contain("secret number 1");
    }

    /// <summary>The request of the SDK prints masked, so it passes the leak check.</summary>
    [Fact]
    public void AssertHidesSecrets_Request_Passes()
    {
        var run = () => TaxContract.AssertHidesSecrets(
            TaxFixtures.Request, TaxFixtures.BuyerTaxId, TaxFixtures.BuyerStreet, null, string.Empty);

        run.Should().NotThrow();
    }

    /// <summary>Rebuilds a correct calculation with every line changed.</summary>
    /// <param name="request">The request to calculate.</param>
    /// <param name="change">The change applied to each correct line.</param>
    /// <returns>A calculation of the changed lines.</returns>
    private static TaxCalculationResult Change(TaxCalculationRequest request, Func<TaxLineResult, TaxLineResult> change)
    {
        return TaxCalculationResult.Calculated(TaxFixtures.Correct(request).Lines!.Select(change).ToList());
    }

    /// <summary>Asserts that a helper call throws a violation whose message contains each given text.</summary>
    /// <param name="run">The helper call.</param>
    /// <param name="expected">The texts the message must contain.</param>
    /// <returns>A task that completes when the check passed.</returns>
    private static async Task AssertViolationAsync(Func<Task> run, params string[] expected)
    {
        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        foreach (var part in expected)
        {
            message.Should().Contain(part);
        }
    }
}
