using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Plugins.VirtualServers;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A virtual server plugin the test scripts, so the contract helper can be shown to accept a well behaved plugin and to
/// reject each way of breaking a promise. By default it behaves like a platform that holds one server: the first create
/// makes it, every repeat finds it, the changes succeed and then are already applied, and the reads answer fixed values.
/// Setting <see cref="HoldsServer"/> to <see langword="false"/> makes it a platform that holds nothing. A script receives
/// the call number of its operation, which starts at 1, and may throw.
/// </summary>
internal sealed class ScriptedVirtualServerPlugin : IVirtualServerListPlugin
{
    /// <summary>The server every default answer carries.</summary>
    private static readonly VirtualServerInstance DefaultInstance = new("srv-1", "192.0.2.30", null, null);

    /// <summary>How many times each operation was called, by method name.</summary>
    private readonly Dictionary<string, int> _calls = new(StringComparer.Ordinal);

    /// <summary>Gets or sets a value telling whether the platform holds the server of the call; <see langword="false"/> makes every read and change answer not found.</summary>
    public bool HoldsServer { get; set; } = true;

    /// <summary>Gets or sets a value telling whether the held server is suspended; a suspended server is not started.</summary>
    public bool IsSuspended { get; set; }

    /// <summary>Gets or sets what <c>ListServersAsync</c> answers for the cursor it is asked; <see langword="null"/> asks for the first page.</summary>
    public Func<string?, PanelAccountListResult>? ListServers { get; set; }

    /// <summary>Gets or sets what <c>CreateServerAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<VirtualServerInstance, PanelRefusalReason>>? Create { get; set; }

    /// <summary>Gets or sets what <c>StartServerAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, PanelActionResult>? Start { get; set; }

    /// <summary>Gets or sets what <c>StopServerAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, PanelActionResult>? Stop { get; set; }

    /// <summary>Gets or sets what <c>RebootServerAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, PanelActionResult>? Reboot { get; set; }

    /// <summary>Gets or sets what <c>RebuildServerAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<VirtualServerRootPassword, PanelRefusalReason>>? Rebuild { get; set; }

    /// <summary>Gets or sets what <c>ResetRootPasswordAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<VirtualServerRootPassword, PanelRefusalReason>>? ResetRootPassword { get; set; }

    /// <summary>Gets or sets what <c>SuspendServerAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, PanelActionResult>? Suspend { get; set; }

    /// <summary>Gets or sets what <c>UnsuspendServerAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, PanelActionResult>? Unsuspend { get; set; }

    /// <summary>Gets or sets what <c>TerminateServerAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, PanelActionResult>? Terminate { get; set; }

    /// <summary>Gets or sets what <c>ChangePlanAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, PanelActionResult>? ChangePlan { get; set; }

    /// <summary>Gets or sets what <c>ListImagesAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<IReadOnlyList<VirtualServerImage>, PanelRefusalReason>>? ListImages { get; set; }

    /// <summary>Gets or sets what <c>ListPlansAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<IReadOnlyList<VirtualServerPlan>, PanelRefusalReason>>? ListPlans { get; set; }

    /// <summary>Gets or sets what <c>CreateConsoleSessionAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<VirtualServerConsole, PanelRefusalReason>>? ConsoleSession { get; set; }

    /// <summary>Gets or sets what <c>GetServerUsageAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, ValueResult<VirtualServerUsage, PanelRefusalReason>>? Usage { get; set; }

    /// <summary>Gets or sets what <c>TestServerConnectionAsync</c> answers on each call; <see langword="null"/> keeps the default.</summary>
    public Func<int, PanelConnectionTestResult>? Test { get; set; }

    /// <summary>Gets how many times an operation was called.</summary>
    /// <param name="operation">The method name, for example <c>CreateServerAsync</c>.</param>
    /// <returns>The number of calls, zero when it was never called.</returns>
    public int CallsOf(string operation)
    {
        return _calls.GetValueOrDefault(operation);
    }

    /// <inheritdoc />
    public Task<ValueResult<VirtualServerInstance, PanelRefusalReason>> CreateServerAsync(VirtualServerCreateContext context, CancellationToken ct)
    {
        var call = Count(nameof(CreateServerAsync));
        if (Create is not null)
        {
            return Task.FromResult(Create(call));
        }

        return Task.FromResult(call == 1 ? ValueResult<VirtualServerInstance, PanelRefusalReason>.Done(DefaultInstance) : ValueResult<VirtualServerInstance, PanelRefusalReason>.AlreadyDone(DefaultInstance));
    }

    /// <inheritdoc />
    public Task<PanelActionResult> StartServerAsync(PanelAccountContext context, CancellationToken ct)
    {
        if (IsSuspended && Start is null)
        {
            Count(nameof(StartServerAsync));
            return Task.FromResult(PanelActionResult.Refused(PanelRefusalReason.AccessDenied));
        }

        return Task.FromResult(Change(Start, nameof(StartServerAsync)));
    }

    /// <inheritdoc />
    public Task<PanelActionResult> StopServerAsync(PanelAccountContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(Stop, nameof(StopServerAsync)));
    }

    /// <inheritdoc />
    public Task<PanelActionResult> RebootServerAsync(PanelAccountContext context, CancellationToken ct)
    {
        var call = Count(nameof(RebootServerAsync));
        return Task.FromResult(Reboot is null ? (HoldsServer ? PanelActionResult.Done() : PanelActionResult.NotFound()) : Reboot(call));
    }

    /// <inheritdoc />
    public Task<PanelActionResult> SuspendServerAsync(PanelSuspendContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(Suspend, nameof(SuspendServerAsync)));
    }

    /// <inheritdoc />
    public Task<PanelActionResult> UnsuspendServerAsync(PanelAccountContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(Unsuspend, nameof(UnsuspendServerAsync)));
    }

    /// <inheritdoc />
    public Task<PanelActionResult> TerminateServerAsync(PanelTerminateContext context, CancellationToken ct)
    {
        var call = Count(nameof(TerminateServerAsync));
        if (Terminate is not null)
        {
            return Task.FromResult(Terminate(call));
        }

        return Task.FromResult(call == 1 && HoldsServer ? PanelActionResult.Done() : PanelActionResult.AlreadyDone());
    }

    /// <inheritdoc />
    public Task<ValueResult<VirtualServerRootPassword, PanelRefusalReason>> RebuildServerAsync(VirtualServerRebuildContext context, CancellationToken ct)
    {
        var call = Count(nameof(RebuildServerAsync));
        return Task.FromResult(Rebuild is null ? DataOrNotFound(new VirtualServerRootPassword(null)) : Rebuild(call));
    }

    /// <inheritdoc />
    public Task<PanelActionResult> ChangePlanAsync(PanelChangePackageContext context, CancellationToken ct)
    {
        return Task.FromResult(Change(ChangePlan, nameof(ChangePlanAsync)));
    }

    /// <inheritdoc />
    public Task<ValueResult<VirtualServerRootPassword, PanelRefusalReason>> ResetRootPasswordAsync(PanelChangePasswordContext context, CancellationToken ct)
    {
        var call = Count(nameof(ResetRootPasswordAsync));
        return Task.FromResult(ResetRootPassword is null ? DataOrNotFound(new VirtualServerRootPassword("generated-secret-201")) : ResetRootPassword(call));
    }

    /// <inheritdoc />
    public Task<ValueResult<VirtualServerConsole, PanelRefusalReason>> CreateConsoleSessionAsync(PanelAccountContext context, CancellationToken ct)
    {
        var call = Count(nameof(CreateConsoleSessionAsync));
        return Task.FromResult(ConsoleSession is null ? DataOrNotFound(new VirtualServerConsole("https://console.example.test/s?t=abc", null)) : ConsoleSession(call));
    }

    /// <inheritdoc />
    public Task<ValueResult<VirtualServerUsage, PanelRefusalReason>> GetServerUsageAsync(PanelAccountContext context, CancellationToken ct)
    {
        var call = Count(nameof(GetServerUsageAsync));
        return Task.FromResult(Usage is null ? DataOrNotFound(new VirtualServerUsage(null, 5.5, 1, 2, 3, 4, null, null, DateTimeOffset.UnixEpoch)) : Usage(call));
    }

    /// <inheritdoc />
    public Task<ValueResult<IReadOnlyList<VirtualServerImage>, PanelRefusalReason>> ListImagesAsync(VirtualServerCatalogContext context, CancellationToken ct)
    {
        var call = Count(nameof(ListImagesAsync));
        return Task.FromResult(ListImages is null
            ? ValueResult<IReadOnlyList<VirtualServerImage>, PanelRefusalReason>.Done([new VirtualServerImage("img-1", "Image One"), new VirtualServerImage("img-2", "Image Two")])
            : ListImages(call));
    }

    /// <inheritdoc />
    public Task<ValueResult<IReadOnlyList<VirtualServerPlan>, PanelRefusalReason>> ListPlansAsync(VirtualServerCatalogContext context, CancellationToken ct)
    {
        var call = Count(nameof(ListPlansAsync));
        return Task.FromResult(ListPlans is null
            ? ValueResult<IReadOnlyList<VirtualServerPlan>, PanelRefusalReason>.Done([new VirtualServerPlan("small", 1, 1, 10), new VirtualServerPlan("large", 4, 8, 80)])
            : ListPlans(call));
    }

    /// <inheritdoc />
    public Task<PanelAccountListResult> ListServersAsync(PanelListAccountsContext context, CancellationToken ct)
    {
        Count(nameof(ListServersAsync));
        return Task.FromResult(ListServers is null ? PanelAccountListResult.Page([], null) : ListServers(context.Cursor));
    }

    /// <inheritdoc />
    public Task<PanelConnectionTestResult> TestServerConnectionAsync(PanelTestConnectionContext context, CancellationToken ct)
    {
        var call = Count(nameof(TestServerConnectionAsync));
        return Task.FromResult(Test is null ? PanelConnectionTestResult.Connected("1.0", 3) : Test(call));
    }

    /// <summary>Answers a change of the server: the script when there is one, otherwise done on the first call and already done after.</summary>
    /// <param name="script">The test's script, or <see langword="null"/>.</param>
    /// <param name="operation">The method name.</param>
    /// <returns>The answer.</returns>
    private PanelActionResult Change(Func<int, PanelActionResult>? script, string operation)
    {
        var call = Count(operation);
        if (script is not null)
        {
            return script(call);
        }

        if (!HoldsServer)
        {
            return PanelActionResult.NotFound();
        }

        return call == 1 ? PanelActionResult.Done() : PanelActionResult.AlreadyDone();
    }

    /// <summary>Gets a successful answer when the platform holds the server, otherwise not found.</summary>
    /// <typeparam name="T">The value read.</typeparam>
    /// <param name="value">The value the platform holds.</param>
    /// <returns>The answer.</returns>
    private ValueResult<T, PanelRefusalReason> DataOrNotFound<T>(T value)
        where T : class
    {
        return HoldsServer ? ValueResult<T, PanelRefusalReason>.Done(value) : ValueResult<T, PanelRefusalReason>.NotFound();
    }

    /// <summary>Counts one call of an operation.</summary>
    /// <param name="operation">The method name.</param>
    /// <returns>The call number of this call, starting at 1.</returns>
    private int Count(string operation)
    {
        var next = _calls.GetValueOrDefault(operation) + 1;
        _calls[operation] = next;
        return next;
    }
}
