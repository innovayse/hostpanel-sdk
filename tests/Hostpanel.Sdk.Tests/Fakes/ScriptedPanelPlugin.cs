using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A hosting panel plugin whose every operation is scripted by the test, so the contract helper can be shown to accept a
/// well behaved plugin and to reject each way of breaking a promise. Each script receives the call number of its
/// operation, which starts at 1, and may throw. By default the plugin behaves like a panel that holds one account: the
/// first create creates it, every later one finds it, and the actions succeed.
/// </summary>
internal sealed class ScriptedPanelPlugin : IProvisioningPanelPlugin
{
    /// <summary>How many times each operation was called, by method name.</summary>
    private readonly Dictionary<string, int> _calls = new(StringComparer.Ordinal);

    /// <summary>Gets or sets what <c>CreateAccountAsync</c> answers on each call.</summary>
    public Func<int, PanelCreateResult> Create { get; set; } =
        call => call == 1 ? PanelCreateResult.Created("acct1") : PanelCreateResult.AlreadyExists("acct1", "example.test");

    /// <summary>Gets or sets what <c>SuspendAccountAsync</c> answers on each call.</summary>
    public Func<int, PanelActionResult> Suspend { get; set; } = call => call == 1 ? PanelActionResult.Done() : PanelActionResult.AlreadyDone();

    /// <summary>Gets or sets what <c>UnsuspendAccountAsync</c> answers on each call.</summary>
    public Func<int, PanelActionResult> Unsuspend { get; set; } = call => call == 1 ? PanelActionResult.Done() : PanelActionResult.AlreadyDone();

    /// <summary>Gets or sets what <c>TerminateAccountAsync</c> answers on each call.</summary>
    public Func<int, PanelActionResult> Terminate { get; set; } = call => call == 1 ? PanelActionResult.Done() : PanelActionResult.AlreadyDone();

    /// <summary>Gets or sets what <c>ChangePackageAsync</c> answers on each call.</summary>
    public Func<int, PanelActionResult> ChangePackage { get; set; } = call => call == 1 ? PanelActionResult.Done() : PanelActionResult.AlreadyDone();

    /// <summary>Gets or sets what <c>ChangePasswordAsync</c> answers on each call.</summary>
    public Func<int, PanelActionResult> ChangePassword { get; set; } = _ => PanelActionResult.Done();

    /// <summary>Gets or sets what <c>CreateSsoLinkAsync</c> answers on each call.</summary>
    public Func<int, PanelSsoResult> Sso { get; set; } = _ => PanelSsoResult.Issued("https://panel.example.test/login?t=abc");

    /// <summary>Gets or sets what <c>GetUsageAsync</c> answers on each call.</summary>
    public Func<int, PanelUsageResult> Usage { get; set; } =
        _ => PanelUsageResult.Read(new PanelUsage(10, 100, 5, null, DateTimeOffset.UnixEpoch));

    /// <summary>Gets or sets what <c>TestConnectionAsync</c> answers on each call.</summary>
    public Func<int, PanelConnectionTestResult> Test { get; set; } = _ => PanelConnectionTestResult.Connected("1.0", 3);

    /// <summary>Gets or sets what <c>ListAccountsAsync</c> answers for the cursor it is asked; <see langword="null"/> asks for the first page.</summary>
    public Func<string?, PanelAccountListResult> List { get; set; } = _ => PanelAccountListResult.Page([], null);

    /// <summary>Gets the server the last <c>CreateAccountAsync</c> call was given, to show that the host's context reaches the plugin.</summary>
    public PanelServerConnection? LastCreateServer { get; private set; }

    /// <summary>Gets how many times an operation was called.</summary>
    /// <param name="operation">The method name, for example <c>CreateAccountAsync</c>.</param>
    /// <returns>The number of calls, zero when it was never called.</returns>
    public int CallsOf(string operation)
    {
        return _calls.GetValueOrDefault(operation);
    }

    /// <inheritdoc />
    public Task<PanelCreateResult> CreateAccountAsync(PanelCreateAccountContext context, CancellationToken ct)
    {
        LastCreateServer = context.Server;
        return Task.FromResult(Create(Count(nameof(CreateAccountAsync))));
    }

    /// <inheritdoc />
    public Task<PanelActionResult> SuspendAccountAsync(PanelSuspendContext context, CancellationToken ct)
    {
        return Task.FromResult(Suspend(Count(nameof(SuspendAccountAsync))));
    }

    /// <inheritdoc />
    public Task<PanelActionResult> UnsuspendAccountAsync(PanelAccountContext context, CancellationToken ct)
    {
        return Task.FromResult(Unsuspend(Count(nameof(UnsuspendAccountAsync))));
    }

    /// <inheritdoc />
    public Task<PanelActionResult> TerminateAccountAsync(PanelTerminateContext context, CancellationToken ct)
    {
        return Task.FromResult(Terminate(Count(nameof(TerminateAccountAsync))));
    }

    /// <inheritdoc />
    public Task<PanelActionResult> ChangePackageAsync(PanelChangePackageContext context, CancellationToken ct)
    {
        return Task.FromResult(ChangePackage(Count(nameof(ChangePackageAsync))));
    }

    /// <inheritdoc />
    public Task<PanelActionResult> ChangePasswordAsync(PanelChangePasswordContext context, CancellationToken ct)
    {
        return Task.FromResult(ChangePassword(Count(nameof(ChangePasswordAsync))));
    }

    /// <inheritdoc />
    public Task<PanelSsoResult> CreateSsoLinkAsync(PanelSsoContext context, CancellationToken ct)
    {
        return Task.FromResult(Sso(Count(nameof(CreateSsoLinkAsync))));
    }

    /// <inheritdoc />
    public Task<PanelUsageResult> GetUsageAsync(PanelAccountContext context, CancellationToken ct)
    {
        return Task.FromResult(Usage(Count(nameof(GetUsageAsync))));
    }

    /// <inheritdoc />
    public Task<PanelConnectionTestResult> TestConnectionAsync(PanelTestConnectionContext context, CancellationToken ct)
    {
        return Task.FromResult(Test(Count(nameof(TestConnectionAsync))));
    }

    /// <inheritdoc />
    public Task<PanelAccountListResult> ListAccountsAsync(PanelListAccountsContext context, CancellationToken ct)
    {
        Count(nameof(ListAccountsAsync));
        return Task.FromResult(List(context.Cursor));
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
