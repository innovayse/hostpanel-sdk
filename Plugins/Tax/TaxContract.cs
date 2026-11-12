using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.Tax;

/// <summary>
/// Calls a plugin author runs in the plugin's own tests to prove it keeps the promises of <see cref="ITaxPlugin"/>
/// that the compiler cannot see: every line answered once, amounts that follow the rates, a refusal and an unavailable
/// service as outcomes instead of exceptions or zero tax, no secret in a result. Each check calls the plugin against a
/// tax service the test has arranged (a stub, or a recorded exchange) and throws
/// <see cref="PluginContractViolationException"/> naming the promise that was broken. Nothing here depends on a test
/// framework.
/// </summary>
public static class TaxContract
{
    /// <summary>The name of the operation, for the messages.</summary>
    private const string Operation = nameof(ITaxPlugin.CalculateTaxAsync);

    /// <summary>The checks this contract shares with the other contracts, worded for a tax service.</summary>
    private static readonly ContractCalls Calls = new("address");

    /// <summary>
    /// Calls <see cref="ITaxPlugin.CalculateTaxAsync"/> once and asserts that the answer is a well formed calculation:
    /// every requested line is answered exactly once and no other line is added, each rate is a percentage between 0
    /// and 100, each amount is not negative and follows the rate and the line total, and each jurisdiction names a
    /// country. The caller arranges a tax service that can calculate the request.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="request">The invoice to calculate.</param>
    /// <param name="tolerance">
    /// How far an amount may be from the exact tax of its rate, to allow for the service's rounding; not negative. The
    /// default is one hundredth, which fits a currency with two decimals; pass the smallest unit of the currency for a
    /// currency with another number of decimals. It applies to each line as a whole: the amount is the tax of the line
    /// total, rounded once, so the quantity of the line does not widen it.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The calculation, so the test can assert more about it.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="plugin"/> or <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The request has no currency, a line without an id, with a negative amount or with a quantity below 1, or two
    /// lines with one id.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tolerance"/> is negative.</exception>
    /// <exception cref="PluginContractViolationException">
    /// The call threw, answered <see langword="null"/>, did not calculate, or broke a rule of a calculation.
    /// </exception>
    public static Task<TaxCalculationResult> AssertCalculationIsWellFormedAsync(
        ITaxPlugin plugin,
        TaxCalculationRequest request,
        decimal tolerance = 0.01m,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ValidateRequest(request);
        ArgumentOutOfRangeException.ThrowIfNegative(tolerance);

        return CalculateAsync(plugin, request, tolerance, 1, ct);
    }

    /// <summary>
    /// Calls <see cref="ITaxPlugin.CalculateTaxAsync"/> repeatedly with one request and asserts that every call is a
    /// well formed calculation and that every call answers the same lines as the first: a calculation changes nothing,
    /// so a retry after a lost answer must not give another tax. The caller arranges a tax service that can calculate
    /// the request.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="request">The invoice to calculate.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="tolerance">
    /// How far an amount may be from the exact tax of its rate; not negative. See
    /// <see cref="AssertCalculationIsWellFormedAsync"/>.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The calculation of the first call.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="plugin"/> or <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The request is malformed, as for <see cref="AssertCalculationIsWellFormedAsync"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="calls"/> is less than 2, or <paramref name="tolerance"/> is negative.
    /// </exception>
    /// <exception cref="PluginContractViolationException">
    /// A call threw, answered <see langword="null"/>, broke a rule of a calculation, or answered other lines than the
    /// first call.
    /// </exception>
    public static Task<TaxCalculationResult> AssertCalculationIsRepeatableAsync(
        ITaxPlugin plugin,
        TaxCalculationRequest request,
        int calls = 2,
        decimal tolerance = 0.01m,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ValidateRequest(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(calls, 2);
        ArgumentOutOfRangeException.ThrowIfNegative(tolerance);

        return RepeatAsync(plugin, request, calls, tolerance, ct);
    }

    /// <summary>
    /// Calls <see cref="ITaxPlugin.CalculateTaxAsync"/> once with a request the tax service cannot calculate (an
    /// address it does not cover, a tax number it rejects) and asserts that the plugin refuses it with a
    /// <see cref="TaxRefusalReason"/>: it neither throws, nor answers a calculation with a guessed or zero rate, nor
    /// reports the service as unavailable. The caller arranges a service that answers the refusal.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="request">A request the service refuses.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The refusal, so the test can assert the reason it arranged.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="plugin"/> or <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The request is malformed, as for <see cref="AssertCalculationIsWellFormedAsync"/>.
    /// </exception>
    /// <exception cref="PluginContractViolationException">
    /// The call threw, answered <see langword="null"/>, or answered anything but a refusal with a reason.
    /// </exception>
    public static Task<TaxCalculationResult> AssertUncalculableRequestIsRefusedAsync(
        ITaxPlugin plugin,
        TaxCalculationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ValidateRequest(request);

        return ExpectAsync(
            plugin,
            request,
            TaxOutcome.Refused,
            "answered a calculation for a request the tax service cannot calculate; a rate is never guessed, so it " +
            "must refuse with a reason",
            ct);
    }

    /// <summary>
    /// Calls <see cref="ITaxPlugin.CalculateTaxAsync"/> once while the tax service cannot be reached and asserts that
    /// the plugin answers <see cref="TaxOutcome.Unavailable"/>: it neither throws, nor turns the failure into a
    /// calculation of zero tax, nor presents it as a refusal of the request. The caller arranges a service that does
    /// not answer (a stub that times out or fails the connection).
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="request">A request the service would calculate if it could be reached.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The answer, so the test can assert more about it.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="plugin"/> or <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The request is malformed, as for <see cref="AssertCalculationIsWellFormedAsync"/>.
    /// </exception>
    /// <exception cref="PluginContractViolationException">
    /// The call threw, answered <see langword="null"/>, or answered anything but an unavailable service.
    /// </exception>
    public static Task<TaxCalculationResult> AssertUnreachableServiceIsUnavailableAsync(
        ITaxPlugin plugin,
        TaxCalculationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ValidateRequest(request);

        return ExpectAsync(
            plugin,
            request,
            TaxOutcome.Unavailable,
            "answered something else while the tax service could not be reached; it must answer Unavailable, never " +
            "zero tax and never a refusal of the request",
            ct);
    }

    /// <summary>
    /// Asserts that the printed form of a request, a result or any other object the plugin produced contains none of
    /// the given secrets. Pass the values the test fed in (the API key, a tax number, a street); the check reads
    /// <c>ToString</c>, which is what a log line or an exception message would show.
    /// </summary>
    /// <param name="value">The object to print.</param>
    /// <param name="secrets">
    /// The secret values that must not appear. A <see langword="null"/> or empty value is skipped.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> or <paramref name="secrets"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="PluginContractViolationException">
    /// The printed form contains one of the secrets. The message does not repeat it.
    /// </exception>
    public static void AssertHidesSecrets(object value, params string?[] secrets)
    {
        Calls.AssertHidesSecrets(value, secrets);
    }

    /// <summary>
    /// Makes one call, checks the shape of the answer and, for a calculation, every rule of its lines.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="request">The invoice to calculate.</param>
    /// <param name="tolerance">How far an amount may be from the exact tax of its rate.</param>
    /// <param name="call">The call number, starting at 1, for the message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The calculation.</returns>
    /// <exception cref="PluginContractViolationException">
    /// The call threw, answered <see langword="null"/>, did not calculate, or broke a rule of a calculation.
    /// </exception>
    private static async Task<TaxCalculationResult> CalculateAsync(
        ITaxPlugin plugin,
        TaxCalculationRequest request,
        decimal tolerance,
        int call,
        CancellationToken ct)
    {
        var result = await Calls.CallAsync(Operation, call, () => plugin.CalculateTaxAsync(request, ct));
        CheckShape(call, result);

        if (result.Outcome != TaxOutcome.Calculated)
        {
            throw new PluginContractViolationException(
                $"{Operation} answered {result.Outcome} on call {call}. The caller must arrange a request the tax " +
                $"service can calculate.");
        }

        CheckLines(call, request, result.Lines!, tolerance);
        return result;
    }

    /// <summary>Makes one call and asserts that the plugin answered a given non calculating outcome.</summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="request">The invoice to calculate.</param>
    /// <param name="expected">The outcome the promise requires.</param>
    /// <param name="otherwise">The end of the message when the outcome is another one.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The answer.</returns>
    /// <exception cref="PluginContractViolationException">
    /// The call threw, answered <see langword="null"/>, broke the shape of an answer, or answered another outcome.
    /// </exception>
    private static async Task<TaxCalculationResult> ExpectAsync(
        ITaxPlugin plugin,
        TaxCalculationRequest request,
        TaxOutcome expected,
        string otherwise,
        CancellationToken ct)
    {
        var result = await Calls.CallAsync(Operation, 1, () => plugin.CalculateTaxAsync(request, ct));
        CheckShape(1, result);

        if (result.Outcome != expected)
        {
            throw new PluginContractViolationException($"{Operation} {otherwise} (it answered {result.Outcome}).");
        }

        return result;
    }

    /// <summary>
    /// Asserts the shape of the answer: the outcome and the reason (checked by the shared checks), and that a
    /// calculation has its lines while a refusal and an unavailable service have none.
    /// </summary>
    /// <param name="call">The call number, starting at 1, for the message.</param>
    /// <param name="result">The answer.</param>
    /// <exception cref="PluginContractViolationException">
    /// The answer does not have the shape of its outcome.
    /// </exception>
    private static void CheckShape(int call, TaxCalculationResult result)
    {
        Calls.CheckShape(Operation, call, result.Outcome, result.Reason, TaxOutcome.Refused);

        if (result.Outcome == TaxOutcome.Calculated && result.Lines is null)
        {
            throw new PluginContractViolationException(
                $"{Operation} calculated without Lines on call {call}; an invoice without lines is an empty list.");
        }

        if (result.Outcome != TaxOutcome.Calculated && result.Lines is not null)
        {
            throw new PluginContractViolationException(
                $"{Operation} answered {result.Outcome} with Lines on call {call}; only a calculation has them.");
        }
    }

    /// <summary>
    /// Asserts the rules of the lines of a calculation: each requested line is answered exactly once and no other is
    /// added, each line is well formed, and its amount follows its rate and the line total.
    /// </summary>
    /// <param name="call">The call number, starting at 1, for the message.</param>
    /// <param name="request">The request that was calculated.</param>
    /// <param name="answered">The lines the plugin answered.</param>
    /// <param name="tolerance">How far an amount may be from the exact tax of its rate.</param>
    /// <exception cref="PluginContractViolationException">
    /// A line is missing, repeated, unknown or malformed, or its amount does not follow its rate.
    /// </exception>
    private static void CheckLines(
        int call,
        TaxCalculationRequest request,
        IReadOnlyList<TaxLineResult> answered,
        decimal tolerance)
    {
        var requested = request.Lines.ToDictionary(line => line.LineId, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in answered)
        {
            if (line is null || string.IsNullOrWhiteSpace(line.LineId))
            {
                throw new PluginContractViolationException(
                    $"{Operation} answered a line without a LineId on call {call}.");
            }

            if (!requested.TryGetValue(line.LineId, out var asked))
            {
                throw new PluginContractViolationException(
                    $"{Operation} answered line '{line.LineId}' on call {call}, which was not requested.");
            }

            if (!seen.Add(line.LineId))
            {
                throw new PluginContractViolationException(
                    $"{Operation} answered line '{line.LineId}' twice on call {call}.");
            }

            CheckLine(call, request.PricesIncludeTax, asked, line, tolerance);
        }

        var missing = requested.Keys.FirstOrDefault(id => !seen.Contains(id));
        if (missing is not null)
        {
            throw new PluginContractViolationException(
                $"{Operation} did not answer line '{missing}' on call {call}. Every line is answered, and a line it " +
                $"cannot rate makes the whole calculation a refusal.");
        }
    }

    /// <summary>
    /// Asserts that one answered line has a defined jurisdiction, a rate in range, an amount that is not negative and
    /// an amount that follows the rate.
    /// </summary>
    /// <param name="call">The call number, starting at 1, for the message.</param>
    /// <param name="pricesIncludeTax">Whether the line total includes the tax.</param>
    /// <param name="asked">The line that was requested.</param>
    /// <param name="line">The line the plugin answered.</param>
    /// <param name="tolerance">How far the amount may be from the exact tax of the rate.</param>
    /// <exception cref="PluginContractViolationException">
    /// The line is malformed or its amount does not follow its rate.
    /// </exception>
    private static void CheckLine(int call, bool pricesIncludeTax, TaxLine asked, TaxLineResult line, decimal tolerance)
    {
        if (line.Jurisdiction is null || !IsCountryCode(line.Jurisdiction.Country))
        {
            throw new PluginContractViolationException(
                $"{Operation} answered line '{line.LineId}' on call {call} without a jurisdiction country; " +
                "it must be an ISO 3166-1 alpha-2 code in upper case.");
        }

        if (line.Rate is < 0m or > 100m)
        {
            throw new PluginContractViolationException(
                $"{Operation} answered line '{line.LineId}' on call {call} with a rate outside 0 to 100; the rate is " +
                $"a percentage.");
        }

        if (line.Amount < 0m)
        {
            throw new PluginContractViolationException(
                $"{Operation} answered line '{line.LineId}' on call {call} with a negative amount.");
        }

        var mode = pricesIncludeTax ? "include" : "exclude";
        var exact = pricesIncludeTax
            ? asked.Amount * line.Rate / (100m + line.Rate)
            : asked.Amount * line.Rate / 100m;
        if (Math.Abs(line.Amount - exact) > tolerance)
        {
            throw new PluginContractViolationException(
                $"{Operation} answered line '{line.LineId}' on call {call} with an amount that does not match its " +
                $"rate and the line total (prices {mode} tax). An amount follows the rate, and a rate the service " +
                $"does not know is a refusal.");
        }
    }

    /// <summary>Tells whether a value is an ISO 3166-1 alpha-2 country code in upper case.</summary>
    /// <param name="country">The value to test.</param>
    /// <returns><see langword="true"/> for two upper case ASCII letters.</returns>
    private static bool IsCountryCode(string? country)
    {
        return country is { Length: 2 } && country.All(char.IsAsciiLetterUpper);
    }

    /// <summary>Tells whether two calculations answer the same lines, whatever the order they list them in.</summary>
    /// <param name="first">The lines of the first calculation.</param>
    /// <param name="second">The lines of the other calculation.</param>
    /// <returns><see langword="true"/> when both hold equal lines for the same ids.</returns>
    private static bool SameLines(IReadOnlyList<TaxLineResult> first, IReadOnlyList<TaxLineResult> second)
    {
        return first.OrderBy(line => line.LineId, StringComparer.Ordinal)
            .SequenceEqual(second.OrderBy(line => line.LineId, StringComparer.Ordinal));
    }

    /// <summary>
    /// Asserts that a request is one the helper can use: the caller builds it, so a mistake is theirs and not the
    /// plugin's.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <exception cref="ArgumentNullException">
    /// The request, one of its parties or its lines is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The currency is empty, a line has no id, a negative amount or a quantity below 1, or two lines share an id.
    /// </exception>
    private static void ValidateRequest(TaxCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Seller);
        ArgumentNullException.ThrowIfNull(request.Buyer);
        ArgumentNullException.ThrowIfNull(request.Lines);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Currency);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in request.Lines)
        {
            ArgumentNullException.ThrowIfNull(line);
            ArgumentException.ThrowIfNullOrWhiteSpace(line.LineId);
            if (line.Amount < 0m)
            {
                throw new ArgumentException(
                    "A line amount is never negative: a discount is taken off the line before it is sent.",
                    nameof(request));
            }

            if (line.Quantity < 1)
            {
                throw new ArgumentException("A line quantity is at least 1.", nameof(request));
            }

            if (!ids.Add(line.LineId))
            {
                throw new ArgumentException("Two lines share one LineId.", nameof(request));
            }
        }
    }
}

    /// <summary>
    /// Makes the repeated calls and asserts that every one is a calculation that answers the first one's lines.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="request">The invoice to calculate.</param>
    /// <param name="calls">How many times to call.</param>
    /// <param name="tolerance">How far an amount may be from the exact tax of its rate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The calculation of the first call.</returns>
    /// <exception cref="PluginContractViolationException">
    /// A call threw, answered <see langword="null"/>, broke a rule of a calculation, or answered other lines than the
    /// first call.
    /// </exception>
    private static async Task<TaxCalculationResult> RepeatAsync(
        ITaxPlugin plugin,
        TaxCalculationRequest request,
        int calls,
        decimal tolerance,
        CancellationToken ct)
    {
        var first = await CalculateAsync(plugin, request, tolerance, 1, ct);
        for (var call = 2; call <= calls; call++)
        {
            var next = await CalculateAsync(plugin, request, tolerance, call, ct);
            if (!SameLines(first.Lines!, next.Lines!))
            {
                throw new PluginContractViolationException(
                    $"{Operation} answered other lines on call {call} than on call 1. A calculation changes nothing, " +
                    $"so a repeat of one request must answer the same.");
            }
        }

        return first;
    }

