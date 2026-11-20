using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Results;
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
    /// <summary>The name of the login details operation, for the messages.</summary>
    private const string LoginOperation = nameof(IProvisioningPanelLoginPlugin.GetLoginDetailsAsync);

    /// <summary>The checks this contract shares with the registrar and virtual server contracts, worded for a panel.</summary>
    private static readonly ContractChecks Checks = new(new ContractWords("account", "an account", "panel", nameof(PanelCreateResult.ProvisioningRef), "the account was created again. A repeat with the same user name must answer AlreadyApplied."));

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
    public static Task<PanelCreateResult> AssertCreateIsIdempotentAsync(
        IProvisioningPanelPlugin plugin,
        PanelCreateAccountContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return Checks.AssertCreateIsIdempotentAsync<PanelCreateResult, PanelRefusalReason>(
            nameof(IProvisioningPanelPlugin.CreateAccountAsync),
            () => plugin.CreateAccountAsync(context, ct),
            calls,
            result => result.ProvisioningRef);
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
        return Checks.AssertIsIdempotentAsync<PanelActionResult, PanelRefusalReason>(nameof(IProvisioningPanelPlugin.SuspendAccountAsync), () => plugin.SuspendAccountAsync(context, ct), calls);
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
        return Checks.AssertIsIdempotentAsync<PanelActionResult, PanelRefusalReason>(nameof(IProvisioningPanelPlugin.UnsuspendAccountAsync), () => plugin.UnsuspendAccountAsync(context, ct), calls);
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
        return Checks.AssertIsIdempotentAsync<PanelActionResult, PanelRefusalReason>(nameof(IProvisioningPanelPlugin.TerminateAccountAsync), () => plugin.TerminateAccountAsync(context, ct), calls);
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
        return Checks.AssertIsIdempotentAsync<PanelActionResult, PanelRefusalReason>(nameof(IProvisioningPanelPlugin.ChangePackageAsync), () => plugin.ChangePackageAsync(context, ct), calls);
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
        return Checks.AssertIsIdempotentAsync<PanelActionResult, PanelRefusalReason>(nameof(IProvisioningPanelPlugin.ChangePasswordAsync), () => plugin.ChangePasswordAsync(context, ct), calls);
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

        await Checks.ExpectAsync<PanelActionResult, PanelRefusalReason>(
            nameof(IProvisioningPanelPlugin.SuspendAccountAsync),
            () => plugin.SuspendAccountAsync(new PanelSuspendContext(server, service, ContractChecks.UnknownTargetActionReason), ct),
            PanelOperationOutcome.NotFound);
        await Checks.ExpectAsync<PanelActionResult, PanelRefusalReason>(
            nameof(IProvisioningPanelPlugin.UnsuspendAccountAsync),
            () => plugin.UnsuspendAccountAsync(unknownAccount, ct),
            PanelOperationOutcome.NotFound);
        await Checks.ExpectAsync<PanelActionResult, PanelRefusalReason>(
            nameof(IProvisioningPanelPlugin.ChangePackageAsync),
            () => plugin.ChangePackageAsync(new PanelChangePackageContext(server, service, package), ct),
            PanelOperationOutcome.NotFound);
        await Checks.ExpectAsync<PanelActionResult, PanelRefusalReason>(
            nameof(IProvisioningPanelPlugin.TerminateAccountAsync),
            () => plugin.TerminateAccountAsync(new PanelTerminateContext(server, service, ContractChecks.UnknownTargetActionReason), ct),
            PanelOperationOutcome.Succeeded,
            PanelOperationOutcome.AlreadyApplied);

        await Checks.ExpectAsync<PanelUsageResult, PanelRefusalReason>(
            nameof(IProvisioningPanelPlugin.GetUsageAsync),
            () => plugin.GetUsageAsync(unknownAccount, ct),
            PanelOperationOutcome.NotFound,
            PanelOperationOutcome.NotSupported);
        await Checks.ExpectAsync<PanelSsoResult, PanelRefusalReason>(
            nameof(IProvisioningPanelPlugin.CreateSsoLinkAsync),
            () => plugin.CreateSsoLinkAsync(new PanelSsoContext(server, service, null), ct),
            PanelOperationOutcome.NotFound,
            PanelOperationOutcome.NotSupported);
    }

    /// <summary>
    /// Calls <see cref="IProvisioningPanelLoginPlugin.GetLoginDetailsAsync"/> for an account the panel holds and asserts that
    /// it answers the details: <c>Succeeded</c> with a non empty user name and an absolute <c>http</c> or <c>https</c> sign-in
    /// address, or <c>NotSupported</c> for a panel that has no sign-in page to show. The caller arranges a panel that holds
    /// the account.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="knownAccount">The server and an account the panel holds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result, so the test can assert more about the details; a not supported answer has none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="knownAccount"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">
    /// The call threw, answered <see langword="null"/>, answered an outcome that does not fit, succeeded without details, or
    /// answered details with an empty user name or a sign-in address that is not an absolute <c>http</c> or <c>https</c> address.
    /// </exception>
    public static async Task<ValueResult<PanelLoginDetails, PanelRefusalReason>> AssertLoginDetailsAreReadableAsync(
        IProvisioningPanelLoginPlugin plugin,
        PanelAccountContext knownAccount,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(knownAccount);

        var result = await Checks.AssertReadAnswersValueAsync<ValueResult<PanelLoginDetails, PanelRefusalReason>, PanelRefusalReason>(
            LoginOperation,
            () => plugin.GetLoginDetailsAsync(knownAccount, ct),
            true);

        if (result.Value is { } details)
        {
            CheckLoginDetails(details);
        }

        return result;
    }

    /// <summary>
    /// Asserts that <see cref="IProvisioningPanelLoginPlugin.GetLoginDetailsAsync"/> answers <c>NotFound</c> or
    /// <c>NotSupported</c> for an account the panel does not hold, and does not throw. The caller arranges a panel that holds
    /// no account under the reference in <paramref name="unknownAccount"/>.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="unknownAccount">The server and a reference the panel does not know.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="unknownAccount"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/> or answered an outcome the promise does not allow.</exception>
    public static Task AssertUnknownAccountLoginIsNotFoundAsync(
        IProvisioningPanelLoginPlugin plugin,
        PanelAccountContext unknownAccount,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(unknownAccount);

        return Checks.ExpectAsync<ValueResult<PanelLoginDetails, PanelRefusalReason>, PanelRefusalReason>(
            LoginOperation,
            () => plugin.GetLoginDetailsAsync(unknownAccount, ct),
            PanelOperationOutcome.NotFound,
            PanelOperationOutcome.NotSupported);
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
    public static Task<PanelConnectionTestResult> AssertTestConnectionAnswersAsync(
        IProvisioningPanelPlugin plugin,
        PanelTestConnectionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return Checks.AssertTestConnectionAnswersAsync(nameof(IProvisioningPanelPlugin.TestConnectionAsync), () => plugin.TestConnectionAsync(context, ct));
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
    public static Task<IReadOnlyList<PanelAccount>> AssertListAccountsIsCompleteAsync(
        IProvisioningPanelPlugin plugin,
        PanelServerConnection server,
        int maxPages = 1000,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(server);

        return Checks.AssertListIsCompleteAsync(
            nameof(IProvisioningPanelPlugin.ListAccountsAsync),
            cursor => plugin.ListAccountsAsync(new PanelListAccountsContext(server, cursor), ct),
            maxPages);
    }

    /// <summary>
    /// Lists the packages of a server and asserts that the listing is well formed: the call succeeds with a list (or is not
    /// supported, and then has no entries to check), every package has a name, no name appears twice and no limit is negative.
    /// The caller arranges a panel that lists its packages.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="server">The server whose packages are listed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The packages the plugin listed, or an empty list when it does not support the listing.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="server"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/>, answered an outcome that does not fit, succeeded without a list, or returned a malformed or repeated package.</exception>
    public static async Task<IReadOnlyList<PanelPackageDetails>> AssertPackagesAreWellFormedAsync(
        IProvisioningPanelPackagesPlugin plugin,
        PanelServerConnection server,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(server);

        const string operation = nameof(IProvisioningPanelPackagesPlugin.ListPackagesAsync);
        var result = await Checks.AssertReadAnswersValueAsync<ValueResult<IReadOnlyList<PanelPackageDetails>, PanelRefusalReason>, PanelRefusalReason>(
            operation,
            () => plugin.ListPackagesAsync(new PanelListPackagesContext(server), ct),
            true);
        var packages = result.Value ?? [];

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var package in packages)
        {
            if (package is null || string.IsNullOrWhiteSpace(package.Name))
            {
                throw new PluginContractViolationException($"{operation} returned a package without a Name.");
            }

            if (package.DiskLimitBytes is < 0 || package.BandwidthLimitBytes is < 0 || package.InodesLimit is < 0
                || package.MailboxesLimit is < 0 || package.DatabasesLimit is < 0 || package.DomainsLimit is < 0)
            {
                throw new PluginContractViolationException($"{operation} returned a package with a negative limit; a limit the panel does not report is null.");
            }

            if (!names.Add(package.Name))
            {
                throw new PluginContractViolationException($"{operation} returned a package name twice. Each package must be listed once.");
            }
        }

        return packages;
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
        Checks.AssertHidesSecrets(value, secrets);
    }

    /// <summary>Asserts that login details can be shown: a user name to sign in with and an address to sign in at.</summary>
    /// <param name="details">The details the plugin answered.</param>
    /// <exception cref="PluginContractViolationException">The user name is empty or the sign-in address is not an absolute <c>http</c> or <c>https</c> address.</exception>
    private static void CheckLoginDetails(PanelLoginDetails details)
    {
        if (string.IsNullOrWhiteSpace(details.Username))
        {
            throw new PluginContractViolationException($"{LoginOperation} answered details without a Username; the client has nothing to sign in with.");
        }

        var isWebAddress = Uri.TryCreate(details.LoginUrl, UriKind.Absolute, out var address)
            && (address.Scheme == Uri.UriSchemeHttps || address.Scheme == Uri.UriSchemeHttp);
        if (!isWebAddress)
        {
            throw new PluginContractViolationException($"{LoginOperation} answered a LoginUrl that is not an absolute http or https address.");
        }
    }
}
