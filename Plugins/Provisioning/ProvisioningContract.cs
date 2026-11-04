using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Checks a plugin author runs in the plugin's own tests to prove it keeps the promises of
/// <see cref="IProvisioningPanelPlugin"/> that the compiler cannot see: idempotent retries, outcomes instead of
/// exceptions, no secret in a result, a complete listing. Each check calls the plugin against a panel the test has
/// arranged (a stub server, or a recorded exchange) and throws <see cref="PluginContractViolationException"/> naming the
/// promise that was broken. Nothing here depends on a test framework.
/// </summary>
public static class ProvisioningContract
{
    /// <summary>
    /// Calls <see cref="IProvisioningPanelPlugin.CreateAccountAsync"/> repeatedly with one context and asserts the promise
    /// of that method: the first call creates the account (or finds the one an earlier attempt created), every later call
    /// answers <see cref="PanelOperationOutcome.AlreadyApplied"/> with the same reference, and no call throws. The caller
    /// arranges a panel that accepts the create (a fresh user name, a known package).
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The create to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call, so the test can assert more about it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, refused, answered an outcome that does not fit, created twice or changed the reference.</exception>
    public static async Task<PanelCreateResult> AssertCreateIsIdempotentAsync(
        IProvisioningPanelPlugin plugin,
        PanelCreateAccountContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfLessThan(calls, 2);

        const string operation = nameof(IProvisioningPanelPlugin.CreateAccountAsync);
        PanelCreateResult? first = null;
        for (var call = 1; call <= calls; call++)
        {
            var result = await CallAsync(operation, call, () => plugin.CreateAccountAsync(context, ct));
            CheckShape(operation, call, result.Outcome, result.Reason);

            if (result.Outcome is not (PanelOperationOutcome.Succeeded or PanelOperationOutcome.AlreadyApplied))
            {
                throw new PluginContractViolationException(
                    $"{operation} answered {result.Outcome} on call {call}. The caller must arrange a create the panel accepts, and a repeat must answer AlreadyApplied.");
            }

            if (string.IsNullOrWhiteSpace(result.ProvisioningRef))
            {
                throw new PluginContractViolationException($"{operation} answered {result.Outcome} without a ProvisioningRef on call {call}.");
            }

            if (first is null)
            {
                first = result;
                continue;
            }

            if (result.Outcome != PanelOperationOutcome.AlreadyApplied)
            {
                throw new PluginContractViolationException(
                    $"{operation} answered {result.Outcome} on call {call}: the account was created again. A repeat with the same user name must answer AlreadyApplied.");
            }

            if (!string.Equals(result.ProvisioningRef, first.ProvisioningRef, StringComparison.Ordinal))
            {
                throw new PluginContractViolationException(
                    $"{operation} answered a different ProvisioningRef on call {call} than on call 1. The reference must be stable across retries.");
            }
        }

        return first!;
    }

    /// <summary>
    /// Calls <see cref="IProvisioningPanelPlugin.SuspendAccountAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges an active account.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The suspend to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<PanelActionResult> AssertSuspendIsIdempotentAsync(
        IProvisioningPanelPlugin plugin,
        PanelSuspendContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);
        return AssertActionIsIdempotentAsync(nameof(IProvisioningPanelPlugin.SuspendAccountAsync), () => plugin.SuspendAccountAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IProvisioningPanelPlugin.UnsuspendAccountAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges a suspended account.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The unsuspend to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<PanelActionResult> AssertUnsuspendIsIdempotentAsync(
        IProvisioningPanelPlugin plugin,
        PanelAccountContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);
        return AssertActionIsIdempotentAsync(nameof(IProvisioningPanelPlugin.UnsuspendAccountAsync), () => plugin.UnsuspendAccountAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IProvisioningPanelPlugin.TerminateAccountAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>: once the account is gone a repeat is a success, never <c>NotFound</c>.
    /// The caller arranges an existing account.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The terminate to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<PanelActionResult> AssertTerminateIsIdempotentAsync(
        IProvisioningPanelPlugin plugin,
        PanelTerminateContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);
        return AssertActionIsIdempotentAsync(nameof(IProvisioningPanelPlugin.TerminateAccountAsync), () => plugin.TerminateAccountAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IProvisioningPanelPlugin.ChangePackageAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges an account and a package the panel knows.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The package change to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<PanelActionResult> AssertChangePackageIsIdempotentAsync(
        IProvisioningPanelPlugin plugin,
        PanelChangePackageContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);
        return AssertActionIsIdempotentAsync(nameof(IProvisioningPanelPlugin.ChangePackageAsync), () => plugin.ChangePackageAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IProvisioningPanelPlugin.ChangePasswordAsync"/> repeatedly with one password and asserts that every
    /// call answers <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges an existing account and a password the
    /// panel accepts.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The password change to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<PanelActionResult> AssertChangePasswordIsIdempotentAsync(
        IProvisioningPanelPlugin plugin,
        PanelChangePasswordContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);
        return AssertActionIsIdempotentAsync(nameof(IProvisioningPanelPlugin.ChangePasswordAsync), () => plugin.ChangePasswordAsync(context, ct), calls);
    }

    /// <summary>
    /// Asserts how the plugin treats an account the panel does not hold. Suspend, unsuspend and change package answer
    /// <c>NotFound</c>; usage and single sign on answer <c>NotFound</c> or <c>NotSupported</c>; terminate answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>, because an account that is not there is already terminated. No call throws.
    /// The caller arranges a panel that holds no account under the reference in <paramref name="unknownAccount"/>.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="unknownAccount">The server and a reference the panel does not know.</param>
    /// <param name="package">A package the panel knows, for the package change.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when every check passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/>, <paramref name="unknownAccount"/> or <paramref name="package"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/> or answered an outcome the promise does not allow.</exception>
    public static async Task AssertUnknownAccountIsNotFoundAsync(
        IProvisioningPanelPlugin plugin,
        PanelAccountContext unknownAccount,
        PanelPackage package,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(unknownAccount);
        ArgumentNullException.ThrowIfNull(package);

        var server = unknownAccount.Server;
        var service = unknownAccount.Service;

        await ExpectActionAsync(
            nameof(IProvisioningPanelPlugin.SuspendAccountAsync),
            () => plugin.SuspendAccountAsync(new PanelSuspendContext(server, service, "contract check"), ct),
            PanelOperationOutcome.NotFound);
        await ExpectActionAsync(
            nameof(IProvisioningPanelPlugin.UnsuspendAccountAsync),
            () => plugin.UnsuspendAccountAsync(unknownAccount, ct),
            PanelOperationOutcome.NotFound);
        await ExpectActionAsync(
            nameof(IProvisioningPanelPlugin.ChangePackageAsync),
            () => plugin.ChangePackageAsync(new PanelChangePackageContext(server, service, package), ct),
            PanelOperationOutcome.NotFound);
        await ExpectActionAsync(
            nameof(IProvisioningPanelPlugin.TerminateAccountAsync),
            () => plugin.TerminateAccountAsync(new PanelTerminateContext(server, service, "contract check"), ct),
            PanelOperationOutcome.Succeeded,
            PanelOperationOutcome.AlreadyApplied);

        var usage = await CallAsync(nameof(IProvisioningPanelPlugin.GetUsageAsync), 1, () => plugin.GetUsageAsync(unknownAccount, ct));
        CheckShape(nameof(IProvisioningPanelPlugin.GetUsageAsync), 1, usage.Outcome, usage.Reason);
        if (usage.Outcome is not (PanelOperationOutcome.NotFound or PanelOperationOutcome.NotSupported))
        {
            throw new PluginContractViolationException(
                $"{nameof(IProvisioningPanelPlugin.GetUsageAsync)} answered {usage.Outcome} for an unknown account; it must answer NotFound or NotSupported.");
        }

        var sso = await CallAsync(
            nameof(IProvisioningPanelPlugin.CreateSsoLinkAsync),
            1,
            () => plugin.CreateSsoLinkAsync(new PanelSsoContext(server, service, null), ct));
        CheckShape(nameof(IProvisioningPanelPlugin.CreateSsoLinkAsync), 1, sso.Outcome, sso.Reason);
        if (sso.Outcome is not (PanelOperationOutcome.NotFound or PanelOperationOutcome.NotSupported))
        {
            throw new PluginContractViolationException(
                $"{nameof(IProvisioningPanelPlugin.CreateSsoLinkAsync)} answered {sso.Outcome} for an unknown account; it must answer NotFound or NotSupported.");
        }
    }

    /// <summary>
    /// Calls <see cref="IProvisioningPanelPlugin.TestConnectionAsync"/> and asserts that it answers instead of throwing,
    /// whatever the server: an unreachable or refusing panel is an outcome. The caller arranges the server (reachable,
    /// wrong credentials, nothing listening) and compares the returned outcome with what it arranged.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The server to test.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result, so the test can assert the outcome it arranged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">
    /// The call threw, answered <see langword="null"/>, answered an outcome that is not defined, a negative account count,
    /// or a version or count together with an outcome other than <see cref="PanelConnectionOutcome.Connected"/>.
    /// </exception>
    public static async Task<PanelConnectionTestResult> AssertTestConnectionAnswersAsync(
        IProvisioningPanelPlugin plugin,
        PanelTestConnectionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        const string operation = nameof(IProvisioningPanelPlugin.TestConnectionAsync);
        var result = await CallAsync(operation, 1, () => plugin.TestConnectionAsync(context, ct));

        if (!Enum.IsDefined(result.Outcome))
        {
            throw new PluginContractViolationException($"{operation} answered {(int)result.Outcome}, which is not a PanelConnectionOutcome.");
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
    /// Follows <see cref="PanelAccountListResult.NextCursor"/> from the first page to the last and asserts that the
    /// listing is well formed: every page succeeds, every account has a reference and a defined status, no reference
    /// appears twice, and the cursor moves on (a page that returns the cursor it was asked for would loop forever). The
    /// caller arranges a panel with several accounts, and with <paramref name="maxPages"/> small enough to be reached only
    /// by a loop.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="server">The server to list.</param>
    /// <param name="maxPages">The most pages to follow before the listing counts as a loop; at least 1.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Every account of every page, in the order the plugin returned them.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="server"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPages"/> is less than 1.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, did not succeed, returned a malformed or repeated account, or kept returning cursors past <paramref name="maxPages"/>.</exception>
    public static async Task<IReadOnlyList<PanelAccount>> AssertListAccountsIsCompleteAsync(
        IProvisioningPanelPlugin plugin,
        PanelServerConnection server,
        int maxPages = 1000,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(server);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPages, 1);

        const string operation = nameof(IProvisioningPanelPlugin.ListAccountsAsync);
        var accounts = new List<PanelAccount>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? cursor = null;
        for (var page = 1; page <= maxPages; page++)
        {
            var asked = cursor;
            var result = await CallAsync(operation, page, () => plugin.ListAccountsAsync(new PanelListAccountsContext(server, asked), ct));
            CheckShape(operation, page, result.Outcome, result.Reason);

            if (result.Outcome != PanelOperationOutcome.Succeeded)
            {
                throw new PluginContractViolationException($"{operation} answered {result.Outcome} on page {page}; the caller must arrange a panel that lists.");
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

    /// <summary>
    /// Asserts that the printed form of a result, a context or any other object the plugin produced contains none of the
    /// given secrets. Pass the values the test fed in (the token, the password) and what the test knows the plugin must
    /// not echo; the check reads <c>ToString</c>, which is what a log line or an exception message would show.
    /// </summary>
    /// <param name="value">The object to print.</param>
    /// <param name="secrets">The secret values that must not appear. A <see langword="null"/> or empty value is skipped.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="secrets"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">The printed form contains one of the secrets. The message does not repeat it.</exception>
    public static void AssertHidesSecrets(object value, params string?[] secrets)
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

    /// <summary>
    /// Calls an action-shaped operation repeatedly and asserts that each call answers <c>Succeeded</c> or <c>AlreadyApplied</c>.
    /// </summary>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">One call of the operation.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered an outcome that does not fit.</exception>
    private static async Task<PanelActionResult> AssertActionIsIdempotentAsync(
        string operation,
        Func<Task<PanelActionResult>> invoke,
        int calls)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(calls, 2);

        PanelActionResult? first = null;
        for (var call = 1; call <= calls; call++)
        {
            var result = await CallAsync(operation, call, invoke);
            CheckShape(operation, call, result.Outcome, result.Reason);

            if (result.Outcome is not (PanelOperationOutcome.Succeeded or PanelOperationOutcome.AlreadyApplied))
            {
                throw new PluginContractViolationException(
                    $"{operation} answered {result.Outcome} on call {call}. The caller must arrange an account the panel accepts the operation for, and a repeat must answer Succeeded or AlreadyApplied.");
            }

            first ??= result;
        }

        return first!;
    }

    /// <summary>Calls an action-shaped operation once and asserts that its outcome is one of the allowed ones.</summary>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <param name="allowed">The outcomes the promise allows.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/> or answered an outcome that is not allowed.</exception>
    private static async Task ExpectActionAsync(string operation, Func<Task<PanelActionResult>> invoke, params PanelOperationOutcome[] allowed)
    {
        var result = await CallAsync(operation, 1, invoke);
        CheckShape(operation, 1, result.Outcome, result.Reason);

        if (!allowed.Contains(result.Outcome))
        {
            throw new PluginContractViolationException(
                $"{operation} answered {result.Outcome} for an unknown account; it must answer {string.Join(" or ", allowed)}.");
        }
    }

    /// <summary>
    /// Calls the plugin once. An exception other than a cancellation is a broken promise (the caller arranged a panel that
    /// answers), and so is a <see langword="null"/> answer.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="call">The call number, starting at 1, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <returns>The plugin's answer.</returns>
    /// <exception cref="PluginContractViolationException">The call threw or answered <see langword="null"/>.</exception>
    private static async Task<T> CallAsync<T>(string operation, int call, Func<Task<T>> invoke)
        where T : class
    {
        T? result;
        try
        {
            result = await invoke();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Only the type is named: the message may carry a panel address or a user name.
            throw new PluginContractViolationException(
                $"{operation} threw on call {call}: {ex.GetType().Name}. A refusal, a missing account and an unsupported operation are outcomes, and a repeat must answer the same as the first call.");
        }

        if (result is null)
        {
            throw new PluginContractViolationException($"{operation} answered null on call {call}.");
        }

        return result;
    }

    /// <summary>Asserts that an outcome is defined and that a reason is present exactly when the panel refused.</summary>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="call">The call number, starting at 1, for the message.</param>
    /// <param name="outcome">The outcome the plugin answered.</param>
    /// <param name="reason">The reason the plugin answered.</param>
    /// <exception cref="PluginContractViolationException">The outcome is not defined, a refusal has no reason, or another outcome has one.</exception>
    private static void CheckShape(string operation, int call, PanelOperationOutcome outcome, PanelRefusalReason? reason)
    {
        if (!Enum.IsDefined(outcome))
        {
            throw new PluginContractViolationException($"{operation} answered {(int)outcome} on call {call}, which is not a PanelOperationOutcome.");
        }

        if (reason is { } given && !Enum.IsDefined(given))
        {
            throw new PluginContractViolationException($"{operation} answered {(int)given} as the reason on call {call}, which is not a PanelRefusalReason.");
        }

        if (outcome == PanelOperationOutcome.Refused && reason is null)
        {
            throw new PluginContractViolationException($"{operation} refused on call {call} without a PanelRefusalReason.");
        }

        if (outcome != PanelOperationOutcome.Refused && reason is not null)
        {
            throw new PluginContractViolationException($"{operation} answered {outcome} with a PanelRefusalReason on call {call}; only a refusal has one.");
        }
    }
}
