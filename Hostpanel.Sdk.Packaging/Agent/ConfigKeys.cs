namespace Hostpanel.Sdk.Packaging.Agent;

/// <summary>
/// What the SSH key agent's configuration format allows in a profile file: the one section, the keys it defines and the
/// folder its helpers live in. The keys are the ones the agent itself reads, so a profile cannot carry a setting the agent
/// would ignore or a typo that silently leaves a safeguard off.
/// </summary>
internal static class ConfigKeys
{
    /// <summary>The only section of the configuration format.</summary>
    internal const string Section = "agent";

    /// <summary>The key that names the provider profile an installation runs.</summary>
    internal const string Provider = "provider";

    /// <summary>The key that binds an installation to one server record; the host sets it, never a package.</summary>
    internal const string Binding = "binding";

    /// <summary>The key that names the program that tells whether an account belongs to the panel.</summary>
    internal const string AccountCheckCommand = "account_check_command";

    /// <summary>The key that names the folder of the provider helpers.</summary>
    internal const string HelperDir = "helper_dir";

    /// <summary>The folder the agent installs its helpers into: a profile may point only at this folder or below it.</summary>
    internal const string StandardHelperDirectory = "/usr/local/libexec/hostpanel-ssh-key-agent";

    /// <summary>Every key a profile may set, as the agent's own defaults define them.</summary>
    internal static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        "min_uid",
        "home_root",
        "deny_accounts",
        "max_keys",
        "audit_log",
        "lock_dir",
        Provider,
        Binding,
        "ssh_group",
        "eligible_list",
        AccountCheckCommand,
        "check_shadow_lock",
        "jail_enrol",
        "jail_root",
        "jail_group",
        "jail_sections",
        "jail_shell",
        "jail_extras",
        "systemd_unit_dir",
        HelperDir,
    };
}
