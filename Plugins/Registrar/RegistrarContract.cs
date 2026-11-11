using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Checks a plugin author runs in the plugin's own tests to prove it keeps the promises of
/// <see cref="IRegistrarOperationsPlugin"/> that the compiler cannot see: idempotent retries, outcomes instead of
/// exceptions, no secret in a result. Each check calls the plugin against a registrar the test has arranged (a stub server,
/// or a recorded exchange) and throws <see cref="PluginContractViolationException"/> naming the promise that was broken.
/// Nothing here depends on a test framework.
/// </summary>
public static class RegistrarContract
{
    /// <summary>A contact for the checks that need one; a registrar that does not hold the domain never reads it.</summary>
    private static readonly RegistrarContact ContractContact = new(
        "Contract",
        "Check",
        null,
        "contract@example.test",
        "+1.5550100",
        "1 Example Street",
        null,
        "Example City",
        "Example State",
        "00000",
        "US");

    /// <summary>
    /// Calls <see cref="IRegistrarOperationsPlugin.RegisterAsync"/> repeatedly with one context and asserts the promise of
    /// that method: the first call registers the domain (or finds the one an earlier attempt registered), every later call
    /// answers <see cref="PanelOperationOutcome.AlreadyApplied"/> with the same reference, and no call throws. The caller
    /// arranges a registrar that accepts the registration.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The registration to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call, so the test can assert more about it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, registered again, or changed the reference.</exception>
    public static Task<RegistrarDataResult<RegistrarOrder>> AssertRegisterIsIdempotentAsync(
        IRegistrarOperationsPlugin plugin,
        RegistrarRegisterContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return AssertOrderIsIdempotentAsync(nameof(IRegistrarOperationsPlugin.RegisterAsync), () => plugin.RegisterAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IRegistrarOperationsPlugin.TransferAsync"/> repeatedly with one context and asserts that the first
    /// call starts the transfer (or finds the one an earlier attempt started) and every later call answers
    /// <see cref="PanelOperationOutcome.AlreadyApplied"/> with the same reference. The caller arranges a registrar that
    /// accepts the transfer.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The transfer to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, started a second transfer, or changed the reference.</exception>
    public static Task<RegistrarDataResult<RegistrarOrder>> AssertTransferIsIdempotentAsync(
        IRegistrarOperationsPlugin plugin,
        RegistrarTransferContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return AssertOrderIsIdempotentAsync(nameof(IRegistrarOperationsPlugin.TransferAsync), () => plugin.TransferAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IRegistrarOperationsPlugin.RenewAsync"/> repeatedly with one context, which must carry the expiry
    /// the host held before the first call, and asserts that the first call renews and every later call answers
    /// <see cref="PanelOperationOutcome.AlreadyApplied"/> with the same reference: the domain is not renewed twice. The
    /// caller arranges a registrar that holds the domain and accepts the renewal.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The renewal to repeat; <see cref="RegistrarRenewContext.CurrentExpiresAt"/> is set.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="context"/> has no <see cref="RegistrarRenewContext.CurrentExpiresAt"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, renewed twice, or changed the reference.</exception>
    public static Task<RegistrarDataResult<RegistrarOrder>> AssertRenewIsIdempotentAsync(
        IRegistrarOperationsPlugin plugin,
        RegistrarRenewContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);
        if (context.CurrentExpiresAt is null)
        {
            throw new ArgumentException("A renewal can only be repeated safely when the context carries the expiry the host held.", nameof(context));
        }

        return AssertOrderIsIdempotentAsync(nameof(IRegistrarOperationsPlugin.RenewAsync), () => plugin.RenewAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IRegistrarOperationsPlugin.SetNameserversAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges a domain and nameservers the registrar accepts.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The change to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<RegistrarActionResult> AssertSetNameserversIsIdempotentAsync(
        IRegistrarOperationsPlugin plugin,
        RegistrarNameserversContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return AssertActionIsIdempotentAsync(nameof(IRegistrarOperationsPlugin.SetNameserversAsync), () => plugin.SetNameserversAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IRegistrarOperationsPlugin.SetAutoRenewAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges a domain the registrar holds.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The switch to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<RegistrarActionResult> AssertSetAutoRenewIsIdempotentAsync(
        IRegistrarOperationsPlugin plugin,
        RegistrarFlagContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return AssertActionIsIdempotentAsync(nameof(IRegistrarOperationsPlugin.SetAutoRenewAsync), () => plugin.SetAutoRenewAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IRegistrarOperationsPlugin.SetWhoisPrivacyAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges a domain the registrar holds.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The switch to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<RegistrarActionResult> AssertSetWhoisPrivacyIsIdempotentAsync(
        IRegistrarOperationsPlugin plugin,
        RegistrarFlagContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return AssertActionIsIdempotentAsync(nameof(IRegistrarOperationsPlugin.SetWhoisPrivacyAsync), () => plugin.SetWhoisPrivacyAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IRegistrarOperationsPlugin.SetRegistrarLockAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges a domain the registrar holds.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The switch to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<RegistrarActionResult> AssertSetRegistrarLockIsIdempotentAsync(
        IRegistrarOperationsPlugin plugin,
        RegistrarFlagContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return AssertActionIsIdempotentAsync(nameof(IRegistrarOperationsPlugin.SetRegistrarLockAsync), () => plugin.SetRegistrarLockAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IRegistrarOperationsPlugin.SetContactAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges a domain the registrar holds and a contact it accepts.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The change to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<RegistrarActionResult> AssertSetContactIsIdempotentAsync(
        IRegistrarOperationsPlugin plugin,
        RegistrarContactContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return AssertActionIsIdempotentAsync(nameof(IRegistrarOperationsPlugin.SetContactAsync), () => plugin.SetContactAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IRegistrarOperationsPlugin.DeleteDnsRecordAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>: once the record is gone a repeat is a success, never <c>NotFound</c>. The
    /// caller arranges a record that exists before the first call.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The removal to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<RegistrarActionResult> AssertDeleteDnsRecordIsIdempotentAsync(
        IRegistrarOperationsPlugin plugin,
        RegistrarDnsRecordRemovalContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return AssertActionIsIdempotentAsync(nameof(IRegistrarOperationsPlugin.DeleteDnsRecordAsync), () => plugin.DeleteDnsRecordAsync(context, ct), calls);
    }

    /// <summary>
    /// Asserts how the plugin treats a domain the registrar does not hold. Every read and every change answers
    /// <c>NotFound</c>, except that the operations a registrar may lack (the authorization code, the DNS records, the
    /// switches, the transfer cancel and prepare) may answer <c>NotSupported</c> instead. No call throws. The caller arranges
    /// a registrar that holds no domain under the name and reference in <paramref name="unknownDomain"/>.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="unknownDomain">The registrar account and a domain it does not hold.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when every check passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="unknownDomain"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/> or answered an outcome the promise does not allow.</exception>
    public static async Task AssertUnknownDomainIsNotFoundAsync(
        IRegistrarOperationsPlugin plugin,
        RegistrarDomainContext unknownDomain,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(unknownDomain);

        var connection = unknownDomain.Connection;
        var domain = unknownDomain.Domain;

        await ExpectDataAsync(nameof(IRegistrarOperationsPlugin.GetNameserversAsync), () => plugin.GetNameserversAsync(unknownDomain, ct), PanelOperationOutcome.NotFound);
        await ExpectDataAsync(nameof(IRegistrarOperationsPlugin.GetContactAsync), () => plugin.GetContactAsync(unknownDomain, ct), PanelOperationOutcome.NotFound);
        await ExpectDataAsync(nameof(IRegistrarOperationsPlugin.CheckDomainActiveAsync), () => plugin.CheckDomainActiveAsync(unknownDomain, ct), PanelOperationOutcome.NotFound);
        await ExpectDataAsync(
            nameof(IRegistrarOperationsPlugin.RenewAsync),
            () => plugin.RenewAsync(new RegistrarRenewContext(connection, domain, 1, null), ct),
            PanelOperationOutcome.NotFound);
        await ExpectDataAsync(
            nameof(IRegistrarOperationsPlugin.GetEppCodeAsync),
            () => plugin.GetEppCodeAsync(unknownDomain, ct),
            PanelOperationOutcome.NotFound,
            PanelOperationOutcome.NotSupported);
        await ExpectDataAsync(
            nameof(IRegistrarOperationsPlugin.GetDnsRecordsAsync),
            () => plugin.GetDnsRecordsAsync(unknownDomain, ct),
            PanelOperationOutcome.NotFound,
            PanelOperationOutcome.NotSupported);
        await ExpectActionAsync(
            nameof(IRegistrarOperationsPlugin.SetNameserversAsync),
            () => plugin.SetNameserversAsync(new RegistrarNameserversContext(connection, domain, ["ns1.example.test", "ns2.example.test"]), ct),
            PanelOperationOutcome.NotFound);
        await ExpectActionAsync(
            nameof(IRegistrarOperationsPlugin.SetContactAsync),
            () => plugin.SetContactAsync(new RegistrarContactContext(connection, domain, ContractContact), ct),
            PanelOperationOutcome.NotFound);
        await ExpectActionAsync(
            nameof(IRegistrarOperationsPlugin.SetAutoRenewAsync),
            () => plugin.SetAutoRenewAsync(new RegistrarFlagContext(connection, domain, true), ct),
            PanelOperationOutcome.NotFound,
            PanelOperationOutcome.NotSupported);
        await ExpectActionAsync(
            nameof(IRegistrarOperationsPlugin.SetWhoisPrivacyAsync),
            () => plugin.SetWhoisPrivacyAsync(new RegistrarFlagContext(connection, domain, true), ct),
            PanelOperationOutcome.NotFound,
            PanelOperationOutcome.NotSupported);
        await ExpectActionAsync(
            nameof(IRegistrarOperationsPlugin.SetRegistrarLockAsync),
            () => plugin.SetRegistrarLockAsync(new RegistrarFlagContext(connection, domain, true), ct),
            PanelOperationOutcome.NotFound,
            PanelOperationOutcome.NotSupported);
        await ExpectActionAsync(
            nameof(IRegistrarOperationsPlugin.CancelTransferAsync),
            () => plugin.CancelTransferAsync(unknownDomain, ct),
            PanelOperationOutcome.NotFound,
            PanelOperationOutcome.NotSupported);
        await ExpectActionAsync(
            nameof(IRegistrarOperationsPlugin.InitiateOutgoingTransferAsync),
            () => plugin.InitiateOutgoingTransferAsync(unknownDomain, ct),
            PanelOperationOutcome.NotFound,
            PanelOperationOutcome.NotSupported);
    }

    /// <summary>
    /// Calls <see cref="IRegistrarOperationsPlugin.TestConnectionAsync"/> and asserts that it answers instead of throwing,
    /// whatever the registrar: an unreachable or refusing registrar is an outcome. The caller arranges the registrar
    /// (reachable, wrong credentials, nothing listening) and compares the returned outcome with what it arranged.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The registrar account to test.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result, so the test can assert the outcome it arranged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">
    /// The call threw, answered <see langword="null"/>, answered an outcome that is not defined, a negative count, or a
    /// version or count together with an outcome other than <see cref="PanelConnectionOutcome.Connected"/>.
    /// </exception>
    public static Task<PanelConnectionTestResult> AssertTestConnectionAnswersAsync(
        IRegistrarOperationsPlugin plugin,
        RegistrarTestConnectionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return ContractChecks.AssertTestConnectionAnswersAsync(nameof(IRegistrarOperationsPlugin.TestConnectionAsync), () => plugin.TestConnectionAsync(context, ct));
    }

    /// <summary>
    /// Asserts that the printed form of a result, a context or any other object the plugin produced contains none of the
    /// given secrets. Pass the values the test fed in (the API key, the authorization code); the check reads
    /// <c>ToString</c>, which is what a log line or an exception message would show.
    /// </summary>
    /// <param name="value">The object to print.</param>
    /// <param name="secrets">The secret values that must not appear. A <see langword="null"/> or empty value is skipped.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="secrets"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">The printed form contains one of the secrets. The message does not repeat it.</exception>
    public static void AssertHidesSecrets(object value, params string?[] secrets)
    {
        ContractChecks.AssertHidesSecrets(value, secrets);
    }

    /// <summary>Repeats an operation that answers an order and asserts that the repeats find the first call's work.</summary>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">One call of the operation.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or broke the promise.</exception>
    private static Task<RegistrarDataResult<RegistrarOrder>> AssertOrderIsIdempotentAsync(
        string operation,
        Func<Task<RegistrarDataResult<RegistrarOrder>>> invoke,
        int calls)
    {
        return ContractChecks.AssertCreateIsIdempotentAsync<RegistrarDataResult<RegistrarOrder>, RegistrarRefusalReason>(
            operation,
            invoke,
            calls,
            result => result.Value?.RegistrarRef);
    }

    /// <summary>Repeats an action and asserts that each call answers <c>Succeeded</c> or <c>AlreadyApplied</c>.</summary>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">One call of the operation.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered an outcome that does not fit.</exception>
    private static Task<RegistrarActionResult> AssertActionIsIdempotentAsync(string operation, Func<Task<RegistrarActionResult>> invoke, int calls)
    {
        return ContractChecks.AssertIsIdempotentAsync<RegistrarActionResult, RegistrarRefusalReason>(operation, invoke, calls);
    }

    /// <summary>Calls an action once on the unknown domain and asserts that its outcome is one of the allowed ones.</summary>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <param name="allowed">The outcomes the promise allows.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/> or answered an outcome that is not allowed.</exception>
    private static Task ExpectActionAsync(string operation, Func<Task<RegistrarActionResult>> invoke, params PanelOperationOutcome[] allowed)
    {
        return ContractChecks.ExpectAsync<RegistrarActionResult, RegistrarRefusalReason>(operation, "domain", invoke, allowed);
    }

    /// <summary>Calls a value operation once on the unknown domain and asserts that its outcome is one of the allowed ones.</summary>
    /// <typeparam name="T">The value the operation answers.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <param name="allowed">The outcomes the promise allows.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/> or answered an outcome that is not allowed.</exception>
    private static Task ExpectDataAsync<T>(string operation, Func<Task<RegistrarDataResult<T>>> invoke, params PanelOperationOutcome[] allowed)
        where T : class
    {
        return ContractChecks.ExpectAsync<RegistrarDataResult<T>, RegistrarRefusalReason>(operation, "domain", invoke, allowed);
    }
}
