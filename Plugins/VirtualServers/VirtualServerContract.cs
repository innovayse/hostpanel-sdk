using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// Checks a plugin author runs in the plugin's own tests to prove it keeps the promises of
/// <see cref="IVirtualServerPlugin"/> that the compiler cannot see: idempotent retries, outcomes instead of exceptions, no
/// secret in a result, a well formed catalogue. Each check calls the plugin against a platform the test has arranged (a
/// stub server, or a recorded exchange) and throws <see cref="PluginContractViolationException"/> naming the promise that
/// was broken. Nothing here depends on a test framework.
/// </summary>
public static class VirtualServerContract
{
    /// <summary>The password the not found check sends; it is never a real one.</summary>
    private const string CheckPassword = "contract-check-password";

    /// <summary>The checks this contract shares with the other contracts, worded for a virtual server platform.</summary>
    private static readonly ContractChecks Checks = new(new ContractWords("server", "a server", "platform", "ProvisioningRef", "the server was created again. A repeat with the same name must answer AlreadyApplied."));

    /// <summary>
    /// Calls <see cref="IVirtualServerPlugin.CreateServerAsync"/> repeatedly with one context and asserts the promise of that
    /// method: the first call creates the server (or finds the one an earlier attempt created), every later call answers
    /// <see cref="PanelOperationOutcome.AlreadyApplied"/> with the same reference, and no call throws. The caller arranges a
    /// platform that accepts the create (a fresh name, a known plan and image).
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The create to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call, so the test can assert more about it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, created again, or changed the reference.</exception>
    public static Task<ValueResult<VirtualServerInstance, PanelRefusalReason>> AssertCreateIsIdempotentAsync(
        IVirtualServerPlugin plugin,
        VirtualServerCreateContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return Checks.AssertCreateIsIdempotentAsync<ValueResult<VirtualServerInstance, PanelRefusalReason>, PanelRefusalReason>(
            nameof(IVirtualServerPlugin.CreateServerAsync),
            () => plugin.CreateServerAsync(context, ct),
            calls,
            result => result.Value?.ProvisioningRef);
    }

    /// <summary>
    /// Calls <see cref="IVirtualServerPlugin.StartServerAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges a stopped server.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The start to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<PanelActionResult> AssertStartIsIdempotentAsync(
        IVirtualServerPlugin plugin,
        PanelAccountContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return Checks.AssertIsIdempotentAsync<PanelActionResult, PanelRefusalReason>(nameof(IVirtualServerPlugin.StartServerAsync), () => plugin.StartServerAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IVirtualServerPlugin.StopServerAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges a running server.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The stop to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<PanelActionResult> AssertStopIsIdempotentAsync(
        IVirtualServerPlugin plugin,
        PanelAccountContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return Checks.AssertIsIdempotentAsync<PanelActionResult, PanelRefusalReason>(nameof(IVirtualServerPlugin.StopServerAsync), () => plugin.StopServerAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IVirtualServerPlugin.SuspendServerAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges an active server.
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
        IVirtualServerPlugin plugin,
        PanelSuspendContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return Checks.AssertIsIdempotentAsync<PanelActionResult, PanelRefusalReason>(nameof(IVirtualServerPlugin.SuspendServerAsync), () => plugin.SuspendServerAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IVirtualServerPlugin.UnsuspendServerAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges a suspended server.
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
        IVirtualServerPlugin plugin,
        PanelAccountContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return Checks.AssertIsIdempotentAsync<PanelActionResult, PanelRefusalReason>(nameof(IVirtualServerPlugin.UnsuspendServerAsync), () => plugin.UnsuspendServerAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IVirtualServerPlugin.TerminateServerAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>: once the server is gone a repeat is a success, never <c>NotFound</c>. The
    /// caller arranges an existing server.
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
        IVirtualServerPlugin plugin,
        PanelTerminateContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return Checks.AssertIsIdempotentAsync<PanelActionResult, PanelRefusalReason>(nameof(IVirtualServerPlugin.TerminateServerAsync), () => plugin.TerminateServerAsync(context, ct), calls);
    }

    /// <summary>
    /// Calls <see cref="IVirtualServerPlugin.ChangePlanAsync"/> repeatedly and asserts that every call answers
    /// <c>Succeeded</c> or <c>AlreadyApplied</c>. The caller arranges a server and a plan the platform knows.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The plan change to repeat.</param>
    /// <param name="calls">How many times to call; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the first call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="calls"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, or answered anything but <c>Succeeded</c> or <c>AlreadyApplied</c>.</exception>
    public static Task<PanelActionResult> AssertChangePlanIsIdempotentAsync(
        IVirtualServerPlugin plugin,
        PanelChangePackageContext context,
        int calls = 3,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return Checks.AssertIsIdempotentAsync<PanelActionResult, PanelRefusalReason>(nameof(IVirtualServerPlugin.ChangePlanAsync), () => plugin.ChangePlanAsync(context, ct), calls);
    }

    /// <summary>
    /// Asserts how the plugin treats a server the platform does not hold. Start, stop, reboot, suspend, unsuspend, change
    /// plan, rebuild and root password reset answer <c>NotFound</c>; the console and usage answer <c>NotFound</c> or
    /// <c>NotSupported</c>; terminate answers only <c>AlreadyApplied</c>, because a server that is not there
    /// is already terminated and nothing was deleted now. No call throws. The caller arranges a platform that holds no server under the reference in
    /// <paramref name="unknownServer"/>.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="unknownServer">The platform and a reference it does not know.</param>
    /// <param name="plan">A plan the platform knows, for the plan change.</param>
    /// <param name="imageId">An image id the platform knows, for the rebuild.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when every check passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/>, <paramref name="unknownServer"/>, <paramref name="plan"/> or <paramref name="imageId"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/> or answered an outcome the promise does not allow.</exception>
    public static async Task AssertUnknownServerIsNotFoundAsync(
        IVirtualServerPlugin plugin,
        PanelAccountContext unknownServer,
        PanelPackage plan,
        string imageId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(unknownServer);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(imageId);

        var server = unknownServer.Server;
        var service = unknownServer.Service;

        await ExpectActionAsync(nameof(IVirtualServerPlugin.StartServerAsync), () => plugin.StartServerAsync(unknownServer, ct), PanelOperationOutcome.NotFound);
        await ExpectActionAsync(nameof(IVirtualServerPlugin.StopServerAsync), () => plugin.StopServerAsync(unknownServer, ct), PanelOperationOutcome.NotFound);
        await ExpectActionAsync(nameof(IVirtualServerPlugin.RebootServerAsync), () => plugin.RebootServerAsync(unknownServer, ct), PanelOperationOutcome.NotFound);
        await ExpectActionAsync(nameof(IVirtualServerPlugin.UnsuspendServerAsync), () => plugin.UnsuspendServerAsync(unknownServer, ct), PanelOperationOutcome.NotFound);
        await ExpectActionAsync(
            nameof(IVirtualServerPlugin.SuspendServerAsync),
            () => plugin.SuspendServerAsync(new PanelSuspendContext(server, service, "contract check"), ct),
            PanelOperationOutcome.NotFound);
        await ExpectActionAsync(
            nameof(IVirtualServerPlugin.ChangePlanAsync),
            () => plugin.ChangePlanAsync(new PanelChangePackageContext(server, service, plan), ct),
            PanelOperationOutcome.NotFound);
        await ExpectActionAsync(
            nameof(IVirtualServerPlugin.TerminateServerAsync),
            () => plugin.TerminateServerAsync(new PanelTerminateContext(server, service, "contract check"), ct),
            PanelOperationOutcome.AlreadyApplied);
        await ExpectDataAsync(
            nameof(IVirtualServerPlugin.RebuildServerAsync),
            () => plugin.RebuildServerAsync(new VirtualServerRebuildContext(server, service, imageId, CheckPassword), ct),
            PanelOperationOutcome.NotFound);
        await ExpectDataAsync(
            nameof(IVirtualServerPlugin.ResetRootPasswordAsync),
            () => plugin.ResetRootPasswordAsync(new PanelChangePasswordContext(server, service, CheckPassword), ct),
            PanelOperationOutcome.NotFound);
        await ExpectDataAsync(
            nameof(IVirtualServerPlugin.CreateConsoleSessionAsync),
            () => plugin.CreateConsoleSessionAsync(unknownServer, ct),
            PanelOperationOutcome.NotFound,
            PanelOperationOutcome.NotSupported);
        await ExpectDataAsync(
            nameof(IVirtualServerPlugin.GetServerUsageAsync),
            () => plugin.GetServerUsageAsync(unknownServer, ct),
            PanelOperationOutcome.NotFound,
            PanelOperationOutcome.NotSupported);
    }

    /// <summary>
    /// Lists the images and the plans and asserts that both catalogues are well formed: each call succeeds with a list (or is not supported, and then has no entries to check), every entry has
    /// an id or a name and a display name, and no id or name appears twice. The caller arranges a platform that lists.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The platform to list.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when both listings passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, answered an outcome that does not fit, succeeded without a list, or returned a malformed or repeated entry.</exception>
    public static async Task AssertCatalogIsWellFormedAsync(
        IVirtualServerPlugin plugin,
        VirtualServerCatalogContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        var images = await ReadCatalogAsync(nameof(IVirtualServerPlugin.ListImagesAsync), () => plugin.ListImagesAsync(context, ct));
        var imageIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var image in images)
        {
            if (image is null || string.IsNullOrWhiteSpace(image.Id) || string.IsNullOrWhiteSpace(image.Name))
            {
                throw new PluginContractViolationException($"{nameof(IVirtualServerPlugin.ListImagesAsync)} returned an image without an Id or a Name.");
            }

            if (!imageIds.Add(image.Id))
            {
                throw new PluginContractViolationException($"{nameof(IVirtualServerPlugin.ListImagesAsync)} returned an image id twice. Each image must be listed once.");
            }
        }

        var plans = await ReadCatalogAsync(nameof(IVirtualServerPlugin.ListPlansAsync), () => plugin.ListPlansAsync(context, ct));
        var planNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var plan in plans)
        {
            if (plan is null || string.IsNullOrWhiteSpace(plan.Name))
            {
                throw new PluginContractViolationException($"{nameof(IVirtualServerPlugin.ListPlansAsync)} returned a plan without a Name.");
            }

            if (!planNames.Add(plan.Name))
            {
                throw new PluginContractViolationException($"{nameof(IVirtualServerPlugin.ListPlansAsync)} returned a plan name twice. Each plan must be listed once.");
            }
        }
    }

    /// <summary>
    /// Calls <see cref="IVirtualServerPlugin.StartServerAsync"/> on a server the host has suspended and asserts that the
    /// plugin refuses with <see cref="PanelRefusalReason.AccessDenied"/>: a suspended server is not started. The caller
    /// arranges a suspended server.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="suspendedServer">The platform and a server that is suspended.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="suspendedServer"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/>, or answered anything but a refusal with <see cref="PanelRefusalReason.AccessDenied"/>.</exception>
    public static async Task AssertSuspendedServerIsNotStartedAsync(
        IVirtualServerPlugin plugin,
        PanelAccountContext suspendedServer,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(suspendedServer);

        const string operation = nameof(IVirtualServerPlugin.StartServerAsync);
        var result = await Checks.CallAsync(operation, 1, () => plugin.StartServerAsync(suspendedServer, ct));
        Checks.CheckShape(operation, 1, result.Outcome, result.Reason);

        if (result.Outcome != PanelOperationOutcome.Refused || result.Reason != PanelRefusalReason.AccessDenied)
        {
            throw new PluginContractViolationException($"{operation} answered {result.Outcome} for a suspended server; it must refuse with AccessDenied.");
        }
    }

    /// <summary>
    /// Asserts that the reads of a server the platform holds answer a value: the usage figures and the console address. A
    /// read that succeeds without a value is a broken promise, and so is any outcome but <c>Succeeded</c>, except that the
    /// usage and the console may be <c>NotSupported</c>, as the interface allows. The caller arranges a platform that holds the server.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="knownServer">The platform and a server it holds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when every check passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="knownServer"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, answered an outcome that does not fit, or succeeded without a value.</exception>
    public static async Task AssertReadsAnswerValuesAsync(
        IVirtualServerPlugin plugin,
        PanelAccountContext knownServer,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(knownServer);

        await Checks.AssertReadAnswersValueAsync<ValueResult<VirtualServerUsage, PanelRefusalReason>, PanelRefusalReason>(
            nameof(IVirtualServerPlugin.GetServerUsageAsync),
            () => plugin.GetServerUsageAsync(knownServer, ct),
            true);
        await Checks.AssertReadAnswersValueAsync<ValueResult<VirtualServerConsole, PanelRefusalReason>, PanelRefusalReason>(
            nameof(IVirtualServerPlugin.CreateConsoleSessionAsync),
            () => plugin.CreateConsoleSessionAsync(knownServer, ct),
            true);
    }

    /// <summary>
    /// Follows <see cref="PanelAccountListResult.NextCursor"/> of <see cref="IVirtualServerListPlugin.ListServersAsync"/> from
    /// the first page to the last and asserts that the listing is well formed: every page succeeds, every server has a
    /// reference and a defined status, no reference appears twice, and the cursor moves on. The caller arranges a platform
    /// with several servers, and with <paramref name="maxPages"/> small enough to be reached only by a loop.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="server">The platform to list.</param>
    /// <param name="maxPages">The most pages to follow before the listing counts as a loop; at least 1.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Every server of every page, in the order the plugin returned them.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="server"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPages"/> is less than 1.</exception>
    /// <exception cref="PluginContractViolationException">A call threw, answered <see langword="null"/>, did not succeed, returned a malformed or repeated server, or kept returning cursors past <paramref name="maxPages"/>.</exception>
    public static Task<IReadOnlyList<PanelAccount>> AssertListServersIsCompleteAsync(
        IVirtualServerListPlugin plugin,
        PanelServerConnection server,
        int maxPages = 1000,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(server);

        return Checks.AssertListIsCompleteAsync(
            nameof(IVirtualServerListPlugin.ListServersAsync),
            cursor => plugin.ListServersAsync(new PanelListAccountsContext(server, cursor), ct),
            maxPages);
    }

    /// <summary>
    /// Calls <see cref="IVirtualServerPlugin.TestServerConnectionAsync"/> and asserts that it answers instead of throwing,
    /// whatever the platform: an unreachable or refusing platform is an outcome. The caller arranges the platform
    /// (reachable, wrong credentials, nothing listening) and compares the returned outcome with what it arranged.
    /// </summary>
    /// <param name="plugin">The plugin under test.</param>
    /// <param name="context">The platform to test.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result, so the test can assert the outcome it arranged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plugin"/> or <paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">
    /// The call threw, answered <see langword="null"/>, answered an outcome that is not defined, a negative count, or a
    /// version or count together with an outcome other than <see cref="PanelConnectionOutcome.Connected"/>.
    /// </exception>
    public static Task<PanelConnectionTestResult> AssertTestConnectionAnswersAsync(
        IVirtualServerPlugin plugin,
        PanelTestConnectionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(context);

        return Checks.AssertTestConnectionAnswersAsync(nameof(IVirtualServerPlugin.TestServerConnectionAsync), () => plugin.TestServerConnectionAsync(context, ct));
    }

    /// <summary>
    /// Asserts that the printed form of a result, a context or any other object the plugin produced contains none of the
    /// given secrets. Pass the values the test fed in (the token, the root password) and what the test knows the plugin must
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

    /// <summary>Calls an action once on the unknown server and asserts that its outcome is one of the allowed ones.</summary>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <param name="allowed">The outcomes the promise allows.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/> or answered an outcome that is not allowed.</exception>
    private static Task ExpectActionAsync(string operation, Func<Task<PanelActionResult>> invoke, params PanelOperationOutcome[] allowed)
    {
        return Checks.ExpectAsync<PanelActionResult, PanelRefusalReason>(operation, invoke, allowed);
    }

    /// <summary>Calls a value operation once on the unknown server and asserts that its outcome is one of the allowed ones.</summary>
    /// <typeparam name="T">The value the operation answers.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <param name="allowed">The outcomes the promise allows.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/> or answered an outcome that is not allowed.</exception>
    private static Task ExpectDataAsync<T>(string operation, Func<Task<ValueResult<T, PanelRefusalReason>>> invoke, params PanelOperationOutcome[] allowed)
        where T : class
    {
        return Checks.ExpectAsync<ValueResult<T, PanelRefusalReason>, PanelRefusalReason>(operation, invoke, allowed);
    }

    /// <summary>
    /// Calls a catalogue operation once and asserts that it answers a list, or is not supported. The caller then checks the
    /// entries; a platform that lacks the catalogue has none to check.
    /// </summary>
    /// <typeparam name="T">The entry type of the catalogue.</typeparam>
    /// <param name="operation">The operation's name, for the message.</param>
    /// <param name="invoke">The call.</param>
    /// <returns>The entries the plugin listed, or an empty list when it does not support the catalogue.</returns>
    /// <exception cref="PluginContractViolationException">The call threw, answered <see langword="null"/>, answered an outcome that does not fit, or succeeded without a list.</exception>
    private static async Task<IReadOnlyList<T>> ReadCatalogAsync<T>(string operation, Func<Task<ValueResult<IReadOnlyList<T>, PanelRefusalReason>>> invoke)
    {
        var result = await Checks.AssertReadAnswersValueAsync<ValueResult<IReadOnlyList<T>, PanelRefusalReason>, PanelRefusalReason>(operation, invoke, true);

        return result.Value ?? [];
    }
}
