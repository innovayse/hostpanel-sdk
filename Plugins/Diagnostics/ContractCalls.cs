using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.Diagnostics;

/// <summary>
/// The checks every contract helper shares whatever its outcome enum: the guarded call, the shape of an outcome and its
/// reason, and the secret scan, for <see cref="Provisioning.ProvisioningContract"/>,
/// <see cref="Registrar.RegistrarContract"/>, <see cref="VirtualServers.VirtualServerContract"/> and
/// <see cref="Tax.TaxContract"/>. <see cref="ContractChecks"/> adds the checks of the operations that change something
/// (idempotency, reads, listings); a contract that only reads, such as the tax contract, uses this class directly and
/// needs no more words than the noun of its target. Every check throws
/// <see cref="PluginContractViolationException"/> naming the promise that was broken, and never repeats a value the
/// plugin was given.
/// </summary>
internal class ContractCalls
{
    /// <summary>Initializes a new instance of the <see cref="ContractCalls"/> class.</summary>
    /// <param name="target">The noun the messages use for the target of the operation.</param>
    internal ContractCalls(string target)
    {
        Target = target;
    }

    /// <summary>
    /// Gets the noun the messages use for what the caller arranges to be missing or present (account, domain,
    /// address).
    /// </summary>
    protected string Target { get; }

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
    internal async Task<T> CallAsync<T>(string operation, int call, Func<Task<T>> invoke)
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
                $"{operation} threw on call {call}: {ex.GetType().Name}. A refusal, a missing {Target} and an unsupported operation are outcomes, and a repeat must answer the same as the first call.");
        }

        if (result is null)
        {
            throw new PluginContractViolationException($"{operation} answered null on call {call}.");
        }

        return result;
    }

    /// <summary>Asserts that an outcome is defined and that a reason is present exactly when the other side refused.</summary>
    /// <typeparam name="TOutcome">The outcome enum of the contract.</typeparam>
    /// <typeparam name="TReason">The refusal reason enum of the contract.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="call">The call number, starting at 1, for the message.</param>
    /// <param name="outcome">The outcome the plugin answered.</param>
    /// <param name="reason">The reason the plugin answered.</param>
    /// <param name="refused">The outcome value that means a refusal, the only one that carries a reason.</param>
    /// <exception cref="PluginContractViolationException">The outcome is not defined, a refusal has no reason, or another outcome has one.</exception>
    internal void CheckShape<TOutcome, TReason>(string operation, int call, TOutcome outcome, TReason? reason, TOutcome refused)
        where TOutcome : struct, Enum
        where TReason : struct, Enum
    {
        var outcomeName = typeof(TOutcome).Name;
        var reasonName = typeof(TReason).Name;
        if (!Enum.IsDefined(outcome))
        {
            throw new PluginContractViolationException($"{operation} answered {Convert.ToInt32(outcome)} on call {call}, which is not a {outcomeName}.");
        }

        if (reason is { } given && !Enum.IsDefined(given))
        {
            throw new PluginContractViolationException($"{operation} answered {Convert.ToInt32(given)} as the reason on call {call}, which is not a {reasonName}.");
        }

        if (EqualityComparer<TOutcome>.Default.Equals(outcome, refused) && reason is null)
        {
            throw new PluginContractViolationException($"{operation} refused on call {call} without a {reasonName}.");
        }

        if (!EqualityComparer<TOutcome>.Default.Equals(outcome, refused) && reason is not null)
        {
            throw new PluginContractViolationException($"{operation} answered {outcome} with a {reasonName} on call {call}; only a refusal has one.");
        }
    }

    /// <summary>Asserts that the printed form of an object contains none of the given secrets.</summary>
    /// <param name="value">The object to print.</param>
    /// <param name="secrets">The secret values that must not appear. A <see langword="null"/> or empty value is skipped.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="secrets"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">The printed form contains one of the secrets. The message does not repeat it.</exception>
    internal void AssertHidesSecrets(object value, string?[] secrets)
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
