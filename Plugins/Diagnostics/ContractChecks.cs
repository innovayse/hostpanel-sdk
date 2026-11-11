using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.Diagnostics;

/// <summary>
/// The checks the contract helpers (<see cref="ProvisioningContract"/>, <see cref="Registrar.RegistrarContract"/> and
/// <see cref="VirtualServers.VirtualServerContract"/>) share, so a promise is checked by one piece of code whichever
/// contract it belongs to. Every check throws <see cref="PluginContractViolationException"/> naming the promise that was
/// broken, and never repeats a value the plugin was given.
/// </summary>
internal static class ContractChecks
{
    /// <summary>
    /// Calls the plugin once. An exception other than a cancellation is a broken promise (the caller arranged a server that
    /// answers), and so is a <see langword="null"/> answer.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="call">The call number, starting at 1, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <returns>The plugin's answer.</returns>
    /// <exception cref="PluginContractViolationException">The call threw or answered <see langword="null"/>.</exception>
    internal static async Task<T> CallAsync<T>(string operation, int call, Func<Task<T>> invoke)
        where T : class
    {
        T? result;
        try
        {
            result = await invoke();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Only the type is named: the message may carry an address or a user name.
            throw new PluginContractViolationException(
                $"{operation} threw on call {call}: {ex.GetType().Name}. A refusal, a missing target and an unsupported operation are outcomes, and a repeat must answer the same as the first call.");
        }

        if (result is null)
        {
            throw new PluginContractViolationException($"{operation} answered null on call {call}.");
        }

        return result;
    }

    /// <summary>Asserts that an outcome is defined and that a reason is present exactly when the other side refused.</summary>
    /// <typeparam name="TReason">The refusal reason enum of the contract.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="call">The call number, starting at 1, for the message.</param>
    /// <param name="outcome">The outcome the plugin answered.</param>
    /// <param name="reason">The reason the plugin answered.</param>
    /// <exception cref="PluginContractViolationException">The outcome is not defined, a refusal has no reason, or another outcome has one.</exception>
    internal static void CheckShape<TReason>(string operation, int call, PanelOperationOutcome outcome, TReason? reason)
        where TReason : struct, Enum
    {
        var reasonName = typeof(TReason).Name;
        if (!Enum.IsDefined(outcome))
        {
            throw new PluginContractViolationException($"{operation} answered {(int)outcome} on call {call}, which is not a {nameof(PanelOperationOutcome)}.");
        }

        if (reason is { } given && !Enum.IsDefined(given))
        {
            throw new PluginContractViolationException($"{operation} answered {Convert.ToInt32(given)} as the reason on call {call}, which is not a {reasonName}.");
        }

        if (outcome == PanelOperationOutcome.Refused && reason is null)
        {
            throw new PluginContractViolationException($"{operation} refused on call {call} without a {reasonName}.");
        }

        if (outcome != PanelOperationOutcome.Refused && reason is not null)
        {
            throw new PluginContractViolationException($"{operation} answered {outcome} with a {reasonName} on call {call}; only a refusal has one.");
        }
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
    internal static async Task<TResult> AssertIsIdempotentAsync<TResult, TReason>(string operation, Func<Task<TResult>> invoke, int calls)
        where TResult : class, IOperationAnswer<TReason>
        where TReason : struct, Enum
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(calls, 2);

        TResult? first = null;
        for (var call = 1; call <= calls; call++)
        {
            var result = await CallAsync(operation, call, invoke);
            CheckShape(operation, call, result.Outcome, result.Reason);

            if (result.Outcome is not (PanelOperationOutcome.Succeeded or PanelOperationOutcome.AlreadyApplied))
            {
                throw new PluginContractViolationException(
                    $"{operation} answered {result.Outcome} on call {call}. The caller must arrange a target the operation is accepted for, and a repeat must answer Succeeded or AlreadyApplied.");
            }

            first ??= result;
        }

        return first!;
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
    internal static async Task<TResult> AssertCreateIsIdempotentAsync<TResult, TReason>(
        string operation,
        Func<Task<TResult>> invoke,
        int calls,
        Func<TResult, string?> referenceOf)
        where TResult : class, IOperationAnswer<TReason>
        where TReason : struct, Enum
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(calls, 2);

        TResult? first = null;
        string? firstReference = null;
        for (var call = 1; call <= calls; call++)
        {
            var result = await CallAsync(operation, call, invoke);
            CheckShape(operation, call, result.Outcome, result.Reason);

            if (result.Outcome is not (PanelOperationOutcome.Succeeded or PanelOperationOutcome.AlreadyApplied))
            {
                throw new PluginContractViolationException(
                    $"{operation} answered {result.Outcome} on call {call}. The caller must arrange a call the other side accepts, and a repeat must answer AlreadyApplied.");
            }

            var reference = referenceOf(result);
            if (string.IsNullOrWhiteSpace(reference))
            {
                throw new PluginContractViolationException($"{operation} answered {result.Outcome} without a reference on call {call}.");
            }

            if (first is null)
            {
                first = result;
                firstReference = reference;
                continue;
            }

            if (result.Outcome != PanelOperationOutcome.AlreadyApplied)
            {
                throw new PluginContractViolationException(
                    $"{operation} answered {result.Outcome} on call {call}: the work was done again. A repeat of the same call must answer AlreadyApplied.");
            }

            if (!string.Equals(reference, firstReference, StringComparison.Ordinal))
            {
                throw new PluginContractViolationException(
                    $"{operation} answered a different reference on call {call} than on call 1. The reference must be stable across retries.");
            }
        }

        return first!;
    }

    /// <summary>Calls an operation once and asserts that its outcome is one of the allowed ones.</summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <typeparam name="TReason">The refusal reason enum of the contract.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="subject">What the caller arranged not to exist (account, domain, server), for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <param name="allowed">The outcomes the promise allows.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/> or answered an outcome that is not allowed.</exception>
    internal static async Task ExpectAsync<TResult, TReason>(
        string operation,
        string subject,
        Func<Task<TResult>> invoke,
        params PanelOperationOutcome[] allowed)
        where TResult : class, IOperationAnswer<TReason>
        where TReason : struct, Enum
    {
        var result = await CallAsync(operation, 1, invoke);
        CheckShape(operation, 1, result.Outcome, result.Reason);

        if (!allowed.Contains(result.Outcome))
        {
            throw new PluginContractViolationException(
                $"{operation} answered {result.Outcome} for an unknown {subject}; it must answer {string.Join(" or ", allowed)}.");
        }
    }

    /// <summary>
    /// Calls a connection test and asserts that it answers instead of throwing, with a defined outcome, a non negative count
    /// and no version or count unless it connected.
    /// </summary>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <returns>The result, so the test can assert the outcome it arranged.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/> or broke the shape of a test result.</exception>
    internal static async Task<PanelConnectionTestResult> AssertTestConnectionAnswersAsync(string operation, Func<Task<PanelConnectionTestResult>> invoke)
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

    /// <summary>Asserts that the printed form of an object contains none of the given secrets.</summary>
    /// <param name="value">The object to print.</param>
    /// <param name="secrets">The secret values that must not appear. A <see langword="null"/> or empty value is skipped.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="secrets"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">The printed form contains one of the secrets. The message does not repeat it.</exception>
    internal static void AssertHidesSecrets(object value, string?[] secrets)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(secrets);

        var printed = value.ToString() ?? string.Empty;
        for (var index = 0; index < secrets.Length; index++)
        {
            var secret = secrets[index];
            if (!string.IsNullOrEmpty(secret) && printed.Contains(secret, StringComparison.Ordinal))
            {
                throw new PluginContractViolationException(
                    $"The printed form of {value.GetType().Name} contains secret number {index + 1}. A result, a context and a log line must print secrets masked.");
            }
        }
    }
}
