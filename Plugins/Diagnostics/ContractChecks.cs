using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.Diagnostics;

/// <summary>
/// The checks the contract helpers (<see cref="ProvisioningContract"/>, <see cref="Registrar.RegistrarContract"/> and
/// <see cref="VirtualServers.VirtualServerContract"/>) share, so a promise is checked by one piece of code whichever
/// contract it belongs to. Every check throws <see cref="PluginContractViolationException"/> naming the promise that was
/// broken, and never repeats a value the plugin was given. Each contract owns one instance, built with its own
/// <see cref="ContractWords"/>, so the messages of a contract keep their own nouns.
/// </summary>
internal sealed class ContractChecks : ContractCalls
{
    /// <summary>The reason text the contracts send with a suspension or a termination they expect the plugin to answer for an unknown target.</summary>
    internal const string Reason = "contract check";

    /// <summary>The nouns the messages of this instance use.</summary>
    private readonly ContractWords _words;

    /// <summary>Initializes a new instance of the <see cref="ContractChecks"/> class.</summary>
    /// <param name="words">The nouns the messages use.</param>
    internal ContractChecks(ContractWords words)
        : base(words.Target)
    {
        _words = words;
    }

    /// <summary>
    /// Calls an operation repeatedly and asserts that each call answers <c>Succeeded</c> or <c>AlreadyApplied</c>.
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <typeparam name="TReason">The refusal reason enum of the contract.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">One call of the operation.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered an outcome that does not fit.</exception>
    internal async Task<TResult> AssertIsIdempotentAsync<TResult, TReason>(string operation, Func<Task<TResult>> invoke, int calls)
        where TResult : class, IOperationAnswer<TReason>
        where TReason : struct, Enum
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(calls, 2);

        var first = await FitStepAsync<TResult, TReason>(operation, 1, invoke);
        for (var call = 2; call <= calls; call++)
        {
            await FitStepAsync<TResult, TReason>(operation, call, invoke);
        }

        return first;
    }

    /// <summary>
    /// Makes one call of an idempotent operation and checks what every call must satisfy: the answer has a defined shape
    /// and is <c>Succeeded</c> or <c>AlreadyApplied</c>.
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <typeparam name="TReason">The refusal reason enum of the contract.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="call">The call number, starting at 1, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <returns>The result of the call.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/>, or answered an outcome that does not fit.</exception>
    private async Task<TResult> FitStepAsync<TResult, TReason>(string operation, int call, Func<Task<TResult>> invoke)
        where TResult : class, IOperationAnswer<TReason>
        where TReason : struct, Enum
    {
        var result = await CallAsync(operation, call, invoke);
        CheckShape(operation, call, result.Outcome, result.Reason, PanelOperationOutcome.Refused);

        if (result.Outcome is not (PanelOperationOutcome.Succeeded or PanelOperationOutcome.AlreadyApplied))
        {
            throw new PluginContractViolationException(
                $"{operation} answered {result.Outcome} on call {call}. The caller must arrange {_words.IndefiniteTarget} the {_words.Other} accepts the operation for, and a repeat must answer Succeeded or AlreadyApplied.");
        }

        return result;
    }

    /// <summary>
    /// Calls an operation that creates or orders something repeatedly and asserts the promise of such an operation: the
    /// first call does it (or finds what an earlier attempt did), every later call answers
    /// <see cref="PanelOperationOutcome.AlreadyApplied"/> with the same reference, and no call throws.
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <typeparam name="TReason">The refusal reason enum of the contract.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">One call of the operation.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="referenceOf">Reads the reference the result answers, or <see langword="null"/> when it has none.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, answered without a reference, repeated the work, or changed the reference.</exception>
    internal async Task<TResult> AssertCreateIsIdempotentAsync<TResult, TReason>(
        string operation,
        Func<Task<TResult>> invoke,
        int calls,
        Func<TResult, string?> referenceOf)
        where TResult : class, IOperationAnswer<TReason>
        where TReason : struct, Enum
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(calls, 2);

        var (first, firstReference) = await OrderStepAsync<TResult, TReason>(operation, 1, invoke, referenceOf);
        for (var call = 2; call <= calls; call++)
        {
            var (result, reference) = await OrderStepAsync<TResult, TReason>(operation, call, invoke, referenceOf);

            if (result.Outcome != PanelOperationOutcome.AlreadyApplied)
            {
                throw new PluginContractViolationException(
                    $"{operation} answered {result.Outcome} on call {call}: {_words.CreatedAgain}");
            }

            if (!string.Equals(reference, firstReference, StringComparison.Ordinal))
            {
                throw new PluginContractViolationException(
                    $"{operation} answered a different {_words.Reference} on call {call} than on call 1. The reference must be stable across retries.");
            }
        }

        return first;
    }

    /// <summary>
    /// Makes one call of a create or an order and checks what every call must satisfy: the answer has a defined shape, it is
    /// a success or an already applied, and it carries a reference.
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <typeparam name="TReason">The refusal reason enum of the contract.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="call">The call number, starting at 1, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <param name="referenceOf">Reads the reference the result answers, or <see langword="null"/> when it has none.</param>
    /// <returns>The result and its reference, which is not empty.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/>, answered an outcome that does not fit, or answered without a reference.</exception>
    private async Task<(TResult Result, string Reference)> OrderStepAsync<TResult, TReason>(
        string operation,
        int call,
        Func<Task<TResult>> invoke,
        Func<TResult, string?> referenceOf)
        where TResult : class, IOperationAnswer<TReason>
        where TReason : struct, Enum
    {
        var result = await CallAsync(operation, call, invoke);
        CheckShape(operation, call, result.Outcome, result.Reason, PanelOperationOutcome.Refused);

        if (result.Outcome is not (PanelOperationOutcome.Succeeded or PanelOperationOutcome.AlreadyApplied))
        {
            throw new PluginContractViolationException(
                $"{operation} answered {result.Outcome} on call {call}. The caller must arrange a create the {_words.Other} accepts, and a repeat must answer AlreadyApplied.");
        }

        var reference = referenceOf(result);
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new PluginContractViolationException($"{operation} answered {result.Outcome} without a {_words.Reference} on call {call}.");
        }

        return (result, reference);
    }

    /// <summary>Calls an operation once and asserts that its outcome is one of the allowed ones.</summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <typeparam name="TReason">The refusal reason enum of the contract.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <param name="allowed">The outcomes the promise allows.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/> or answered an outcome that is not allowed.</exception>
    internal async Task ExpectAsync<TResult, TReason>(string operation, Func<Task<TResult>> invoke, params PanelOperationOutcome[] allowed)
        where TResult : class, IOperationAnswer<TReason>
        where TReason : struct, Enum
    {
        var result = await CallAsync(operation, 1, invoke);
        CheckShape(operation, 1, result.Outcome, result.Reason, PanelOperationOutcome.Refused);

        if (!allowed.Contains(result.Outcome))
        {
            throw new PluginContractViolationException(
                $"{operation} answered {result.Outcome} for an unknown {Target}; it must answer {string.Join(" or ", allowed)}.");
        }
    }

    /// <summary>
    /// Calls a read or a list once and asserts that it answers a value: <c>Succeeded</c> with a value, or, when the other
    /// side may lack the operation, <c>NotSupported</c>. A success without a value would leave the host with nothing to show.
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <typeparam name="TReason">The refusal reason enum of the contract.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <param name="mayBeUnsupported"><see langword="true"/> when the other side may lack the operation.</param>
    /// <returns>The result the plugin answered, so the caller can read its value; a not supported answer has none.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/>, answered an outcome that does not fit, or succeeded without a value.</exception>
    internal async Task<TResult> AssertReadAnswersValueAsync<TResult, TReason>(string operation, Func<Task<TResult>> invoke, bool mayBeUnsupported)
        where TResult : class, IOperationAnswer<TReason>, IValueAnswer
        where TReason : struct, Enum
    {
        var result = await CallAsync(operation, 1, invoke);
        CheckShape(operation, 1, result.Outcome, result.Reason, PanelOperationOutcome.Refused);

        if (result.Outcome == PanelOperationOutcome.NotSupported && mayBeUnsupported)
        {
            return result;
        }

        if (result.Outcome != PanelOperationOutcome.Succeeded)
        {
            throw new PluginContractViolationException(
                $"{operation} answered {result.Outcome}; the caller must arrange {_words.IndefiniteTarget} the {_words.Other} reads, and a read answers Succeeded{(mayBeUnsupported ? " or NotSupported" : string.Empty)}.");
        }

        if (result.Value is null)
        {
            throw new PluginContractViolationException($"{operation} answered Succeeded without a value; a read that succeeded must answer what it read, and an empty list is an empty list.");
        }

        return result;
    }

    /// <summary>
    /// Calls a connection test and asserts that it answers instead of throwing, with a defined outcome, a non negative count
    /// and no version or count unless it connected.
    /// </summary>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <returns>The result, so the test can assert the outcome it arranged.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/> or broke the shape of a test result.</exception>
    internal async Task<PanelConnectionTestResult> AssertTestConnectionAnswersAsync(string operation, Func<Task<PanelConnectionTestResult>> invoke)
    {
        var result = await CallAsync(operation, 1, invoke);

        if (!Enum.IsDefined(result.Outcome))
        {
            throw new PluginContractViolationException($"{operation} answered {(int)result.Outcome}, which is not a {nameof(PanelConnectionOutcome)}.");
        }

        if (result.AccountCount is < 0)
        {
            throw new PluginContractViolationException($"{operation} answered a negative AccountCount.");
        }

        if (result.Outcome != PanelConnectionOutcome.Connected && (result.Version is not null || result.AccountCount is not null))
        {
            throw new PluginContractViolationException($"{operation} answered {result.Outcome} with a Version or an AccountCount; only a connected test has them.");
        }

        return result;
    }

    /// <summary>
    /// Follows <see cref="PanelAccountListResult.NextCursor"/> from the first page to the last and asserts that the listing
    /// is well formed: every page succeeds, every account has a reference and a defined status, no reference appears twice,
    /// and the cursor moves on (a page that returns the cursor it was asked for would loop forever).
    /// </summary>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="list">Asks for one page, given the cursor of the previous one or <see langword="null"/> for the first.</param>
    /// <param name="maxPages">The most pages to follow before the listing counts as a loop; at least 1.</param>
    /// <returns>Every account of every page, in the order the plugin returned them.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPages"/> is less than 1.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, did not succeed, returned a malformed or repeated account, or kept returning cursors past <paramref name="maxPages"/>.</exception>
    internal async Task<IReadOnlyList<PanelAccount>> AssertListIsCompleteAsync(
        string operation,
        Func<string?, Task<PanelAccountListResult>> list,
        int maxPages)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPages, 1);

        var accounts = new List<PanelAccount>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? cursor = null;
        for (var page = 1; page <= maxPages; page++)
        {
            var asked = cursor;
            var result = await CallAsync(operation, page, () => list(asked));
            CheckShape(operation, page, result.Outcome, result.Reason, PanelOperationOutcome.Refused);

            if (result.Outcome != PanelOperationOutcome.Succeeded)
            {
                throw new PluginContractViolationException($"{operation} answered {result.Outcome} on page {page}; the caller must arrange a {_words.Other} that lists.");
            }

            if (result.Accounts is null)
            {
                throw new PluginContractViolationException($"{operation} answered null Accounts on page {page}; an empty page is an empty list.");
            }

            foreach (var account in result.Accounts)
            {
                if (account is null || string.IsNullOrWhiteSpace(account.ProvisioningRef))
                {
                    throw new PluginContractViolationException($"{operation} returned an account without a ProvisioningRef on page {page}.");
                }

                if (!Enum.IsDefined(account.Status))
                {
                    throw new PluginContractViolationException($"{operation} returned an account with a Status that is not a PanelAccountStatus on page {page}.");
                }

                if (!seen.Add(account.ProvisioningRef))
                {
                    throw new PluginContractViolationException($"{operation} returned an account twice (page {page}). Following the cursors must return every account exactly once.");
                }

                accounts.Add(account);
            }

            if (result.NextCursor is null)
            {
                return accounts;
            }

            if (string.Equals(result.NextCursor, asked, StringComparison.Ordinal))
            {
                throw new PluginContractViolationException($"{operation} answered the cursor it was asked for on page {page}; the listing would never end.");
            }

            cursor = result.NextCursor;
        }

        throw new PluginContractViolationException($"{operation} was still returning cursors after {maxPages} pages.");
    }
}
