using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Plugins.VirtualServers;
using Hostpanel.Sdk.Tests.Fakes;
using static Hostpanel.Sdk.Tests.Support.ContractViolationAsserts;

namespace Hostpanel.Sdk.Tests.VirtualServers;

/// <summary>
/// Tests of the optional additions to the virtual server contract: the key on a rebuild, the product options, the lost password
/// of a repeated create, and the snapshot, backup and firewall interfaces with their contract checks.
/// </summary>
public sealed class VirtualServerOptionalAdditionsTests
{
    /// <summary>The key on a rebuild and the product options on a create are null by default and are set with an initializer, so the constructors keep their shape.</summary>
    [Fact]
    public void Contexts_NewMembers_AreOptionalInitMembers()
    {
        var rebuild = VirtualServerFixtures.Rebuild with { SshPublicKey = "ssh-ed25519 AAAAexample" };
        var create = VirtualServerFixtures.Create with { ProductOptions = new Dictionary<string, string> { ["backups"] = "daily" } };

        VirtualServerFixtures.Rebuild.SshPublicKey.Should().BeNull();
        VirtualServerFixtures.Create.ProductOptions.Should().BeNull();
        rebuild.SshPublicKey.Should().Be("ssh-ed25519 AAAAexample");
        create.ProductOptions.Should().ContainKey("backups").WhoseValue.Should().Be("daily");
        typeof(VirtualServerRebuildContext).GetConstructors().Should().ContainSingle().Which.GetParameters().Should().HaveCount(4);
        typeof(VirtualServerCreateContext).GetConstructors().Should().ContainSingle().Which.GetParameters().Should().HaveCount(8);
    }

    /// <summary>The contexts still print their passwords masked after they gained a member.</summary>
    [Fact]
    public void Contexts_WithNewMembers_StillMaskThePassword()
    {
        var rebuild = (VirtualServerFixtures.Rebuild with { SshPublicKey = "ssh-ed25519 AAAAexample" }).ToString();
        var instance = new VirtualServerInstance("srv-1", null, null, null) { PasswordUnknown = true }.ToString();

        rebuild.Should().NotContain(VirtualServerFixtures.RebuildPassword).And.Contain("RootPassword = ***");
        instance.Should().Contain("PasswordUnknown = True").And.Contain("GeneratedPassword = null");
    }

    /// <summary>A repeated create that reports its lost password passes the check and returns the instance.</summary>
    [Fact]
    public async Task AssertLostPassword_Reported_Passes()
    {
        var plugin = new ScriptedVirtualServerPlugin { Create = _ => Lost(new VirtualServerInstance("srv-1", null, null, null) { PasswordUnknown = true }) };

        var instance = await VirtualServerContract.AssertLostPasswordIsReportedAsync(plugin, VirtualServerFixtures.Create);

        instance.PasswordUnknown.Should().BeTrue();
    }

    /// <summary>A repeat that hides the loss, answers the wrong outcome or answers a password next to the flag is rejected.</summary>
    [Fact]
    public async Task AssertLostPassword_Broken_Throws()
    {
        var silent = new ScriptedVirtualServerPlugin { Create = _ => Lost(new VirtualServerInstance("srv-1", null, null, null)) };
        var created = new ScriptedVirtualServerPlugin
        {
            Create = _ => ValueResult<VirtualServerInstance, PanelRefusalReason>.Done(new VirtualServerInstance("srv-1", null, null, null) { PasswordUnknown = true }),
        };
        var both = new ScriptedVirtualServerPlugin { Create = _ => Lost(new VirtualServerInstance("srv-1", null, null, "again") { PasswordUnknown = true }) };

        await AssertViolationAsync(() => VirtualServerContract.AssertLostPasswordIsReportedAsync(silent, VirtualServerFixtures.Create), "did not set PasswordUnknown");
        await AssertViolationAsync(() => VirtualServerContract.AssertLostPasswordIsReportedAsync(created, VirtualServerFixtures.Create), "must answer AlreadyApplied");
        await AssertViolationAsync(() => VirtualServerContract.AssertLostPasswordIsReportedAsync(both, VirtualServerFixtures.Create), "together with PasswordUnknown");
    }

    /// <summary>The capability names the manifest lists are fixed, and each new interface extends the virtual server interface without changing it.</summary>
    [Fact]
    public void NewInterfaces_ExtendTheVirtualServerInterface_AndHaveTheirCapabilities()
    {
        PluginCapabilities.VirtualServerSnapshots.Should().Be("virtualServerSnapshots");
        PluginCapabilities.VirtualServerBackups.Should().Be("virtualServerBackups");
        PluginCapabilities.VirtualServerFirewall.Should().Be("virtualServerFirewall");
        typeof(IVirtualServerSnapshotPlugin).GetInterfaces().Should().Contain(typeof(IVirtualServerPlugin));
        typeof(IVirtualServerBackupPlugin).GetInterfaces().Should().Contain(typeof(IVirtualServerPlugin));
        typeof(IVirtualServerFirewallPlugin).GetInterfaces().Should().Contain(typeof(IVirtualServerPlugin));
        typeof(IVirtualServerSnapshotPlugin).GetMethods().Should().HaveCount(4);
        typeof(IVirtualServerBackupPlugin).GetMethods().Should().HaveCount(4);
        typeof(IVirtualServerFirewallPlugin).GetMethods().Should().HaveCount(2);
        typeof(IVirtualServerPlugin).GetMethod("ListSnapshotsAsync").Should().BeNull();
    }

    /// <summary>A snapshot create and delete that finish in the background pass, and the helper calls them as often as asked.</summary>
    [Fact]
    public async Task AssertSnapshotChanges_PendingThenDone_Pass()
    {
        var plugin = new ScriptedVirtualServerPlugin
        {
            CreateSnapshot = call => call == 1 ? PanelActionResult.Pending() : PanelActionResult.AlreadyDone(),
            DeleteSnapshot = call => call == 1 ? PanelActionResult.Done() : PanelActionResult.AlreadyDone(),
        };

        var create = await VirtualServerContract.AssertCreateSnapshotIsIdempotentAsync(plugin, Create, calls: 4);
        var delete = await VirtualServerContract.AssertDeleteSnapshotIsIdempotentAsync(plugin, Point);

        create.Outcome.Should().Be(PanelOperationOutcome.Pending);
        delete.Outcome.Should().Be(PanelOperationOutcome.Succeeded);
        plugin.CallsOf(nameof(IVirtualServerSnapshotPlugin.CreateSnapshotAsync)).Should().Be(4);
    }

    /// <summary>A snapshot delete that answers not found on a repeat is rejected, because a gone snapshot is already deleted.</summary>
    [Fact]
    public async Task AssertDeleteSnapshot_NotFoundOnRepeat_Throws()
    {
        var plugin = new ScriptedVirtualServerPlugin { DeleteSnapshot = ScriptedAnswers.PanelDoneThenNotFound };

        var run = () => VirtualServerContract.AssertDeleteSnapshotIsIdempotentAsync(plugin, Point);

        await AssertViolationAsync(run, "DeleteSnapshotAsync answered NotFound on call 2");
    }

    /// <summary>A well formed snapshot listing passes, and a malformed or repeated entry is rejected.</summary>
    [Fact]
    public async Task AssertSnapshots_WellFormedAndMalformed()
    {
        var good = new ScriptedVirtualServerPlugin { ListSnapshots = _ => Points(new VirtualServerRestorePoint("s1", "before upgrade", DateTimeOffset.UnixEpoch, 10)) };
        var twice = new ScriptedVirtualServerPlugin { ListSnapshots = _ => Points(new VirtualServerRestorePoint("s1", "a", null, null), new VirtualServerRestorePoint("s1", "b", null, null)) };
        var unnamed = new ScriptedVirtualServerPlugin { ListSnapshots = _ => Points(new VirtualServerRestorePoint("s1", "", null, null)) };
        var negative = new ScriptedVirtualServerPlugin { ListSnapshots = _ => Points(new VirtualServerRestorePoint("s1", "a", null, -1)) };

        (await VirtualServerContract.AssertSnapshotsAreWellFormedAsync(good, PanelFixtures.Account)).Should().ContainSingle();
        await AssertViolationAsync(() => VirtualServerContract.AssertSnapshotsAreWellFormedAsync(twice, PanelFixtures.Account), "id twice");
        await AssertViolationAsync(() => VirtualServerContract.AssertSnapshotsAreWellFormedAsync(unnamed, PanelFixtures.Account), "without an Id or a Name");
        await AssertViolationAsync(() => VirtualServerContract.AssertSnapshotsAreWellFormedAsync(negative, PanelFixtures.Account), "negative SizeBytes");
    }

    /// <summary>The backup checks accept a well behaved plugin and reject a malformed listing, with the backup's own operation names in the message.</summary>
    [Fact]
    public async Task AssertBackups_Checks()
    {
        var plugin = new ScriptedVirtualServerPlugin
        {
            CreateBackup = call => call == 1 ? PanelActionResult.Pending() : PanelActionResult.AlreadyDone(),
            ListBackups = _ => Points(new VirtualServerRestorePoint("b1", "nightly", null, null)),
        };
        var broken = new ScriptedVirtualServerPlugin { ListBackups = _ => Points(new VirtualServerRestorePoint("b1", "a", null, null), new VirtualServerRestorePoint("b1", "b", null, null)) };

        (await VirtualServerContract.AssertCreateBackupIsIdempotentAsync(plugin, Create)).Outcome.Should().Be(PanelOperationOutcome.Pending);
        (await VirtualServerContract.AssertDeleteBackupIsIdempotentAsync(plugin, Point)).Outcome.Should().Be(PanelOperationOutcome.Succeeded);
        (await VirtualServerContract.AssertBackupsAreWellFormedAsync(plugin, PanelFixtures.Account)).Should().ContainSingle();
        await AssertViolationAsync(() => VirtualServerContract.AssertBackupsAreWellFormedAsync(broken, PanelFixtures.Account), "ListBackupsAsync returned an id twice");
    }

    /// <summary>A well formed set of firewall rules passes and an empty set is a server without rules.</summary>
    [Fact]
    public async Task AssertFirewall_WellFormed_Passes()
    {
        var rule = new VirtualServerFirewallRule(VirtualServerFirewallDirection.Inbound, VirtualServerFirewallAction.Allow, VirtualServerFirewallProtocol.Tcp, 80, 443, "192.0.2.0/24", null);
        var icmp = new VirtualServerFirewallRule(VirtualServerFirewallDirection.Inbound, VirtualServerFirewallAction.Deny, VirtualServerFirewallProtocol.Icmp, null, null, null, "no ping");
        var plugin = new ScriptedVirtualServerPlugin { GetFirewallRules = _ => Rules(rule, icmp) };

        (await VirtualServerContract.AssertFirewallRulesAreWellFormedAsync(plugin, PanelFixtures.Account)).Should().HaveCount(2);
        (await VirtualServerContract.AssertFirewallRulesAreWellFormedAsync(new ScriptedVirtualServerPlugin(), PanelFixtures.Account)).Should().BeEmpty();
    }

    /// <summary>A rule with ports on a protocol that has none, a port out of range, a range that runs backwards or an undefined value is rejected.</summary>
    [Theory]
    [InlineData(VirtualServerFirewallProtocol.Icmp, 80, null, "with ports")]
    [InlineData(VirtualServerFirewallProtocol.Any, null, 80, "with ports")]
    [InlineData(VirtualServerFirewallProtocol.Tcp, null, 80, "PortTo and no PortFrom")]
    [InlineData(VirtualServerFirewallProtocol.Tcp, 0, null, "outside 1 to 65535")]
    [InlineData(VirtualServerFirewallProtocol.Udp, 70000, null, "outside 1 to 65535")]
    [InlineData(VirtualServerFirewallProtocol.Tcp, 443, 80, "runs backwards")]
    [InlineData((VirtualServerFirewallProtocol)99, null, null, "not defined")]
    public async Task AssertFirewall_MalformedRule_Throws(VirtualServerFirewallProtocol protocol, int? from, int? to, string expected)
    {
        var rule = new VirtualServerFirewallRule(VirtualServerFirewallDirection.Outbound, VirtualServerFirewallAction.Allow, protocol, from, to, null, null);
        var plugin = new ScriptedVirtualServerPlugin { GetFirewallRules = _ => Rules(rule) };

        var run = () => VirtualServerContract.AssertFirewallRulesAreWellFormedAsync(plugin, PanelFixtures.Account);

        await AssertViolationAsync(run, expected);
    }

    /// <summary>Setting the same rules again passes when the platform answers pending and then already applied, and fails when it refuses.</summary>
    [Fact]
    public async Task AssertSetFirewall_Idempotent()
    {
        var context = new VirtualServerFirewallContext(PanelFixtures.Server, PanelFixtures.Service, []);
        var plugin = new ScriptedVirtualServerPlugin { SetFirewallRules = call => call == 1 ? PanelActionResult.Pending() : PanelActionResult.AlreadyDone() };
        var refusing = new ScriptedVirtualServerPlugin { SetFirewallRules = _ => PanelActionResult.Refused(PanelRefusalReason.LimitReached) };

        (await VirtualServerContract.AssertSetFirewallRulesIsIdempotentAsync(plugin, context)).Outcome.Should().Be(PanelOperationOutcome.Pending);
        await AssertViolationAsync(() => VirtualServerContract.AssertSetFirewallRulesIsIdempotentAsync(refusing, context), "answered Refused on call 1");
    }

    /// <summary>Gets a create of a restore point named for the fixture service.</summary>
    private static VirtualServerRestorePointCreateContext Create { get; } = new(PanelFixtures.Server, PanelFixtures.Service, "before-upgrade");

    /// <summary>Gets a restore or delete of one restore point of the fixture service.</summary>
    private static VirtualServerRestorePointContext Point { get; } = new(PanelFixtures.Server, PanelFixtures.Service, "s1");

    /// <summary>Builds the answer of a repeated create.</summary>
    /// <param name="instance">The server the platform already holds.</param>
    /// <returns>An already applied answer.</returns>
    private static ValueResult<VirtualServerInstance, PanelRefusalReason> Lost(VirtualServerInstance instance)
    {
        return ValueResult<VirtualServerInstance, PanelRefusalReason>.AlreadyDone(instance);
    }

    /// <summary>Builds a successful listing of restore points.</summary>
    /// <param name="points">The restore points the listing answers.</param>
    /// <returns>The answer.</returns>
    private static ValueResult<IReadOnlyList<VirtualServerRestorePoint>, PanelRefusalReason> Points(params VirtualServerRestorePoint[] points)
    {
        return ValueResult<IReadOnlyList<VirtualServerRestorePoint>, PanelRefusalReason>.Done(points);
    }

    /// <summary>Builds a successful answer of firewall rules.</summary>
    /// <param name="rules">The rules the platform has.</param>
    /// <returns>The answer.</returns>
    private static ValueResult<IReadOnlyList<VirtualServerFirewallRule>, PanelRefusalReason> Rules(params VirtualServerFirewallRule[] rules)
    {
        return ValueResult<IReadOnlyList<VirtualServerFirewallRule>, PanelRefusalReason>.Done(rules);
    }
}
