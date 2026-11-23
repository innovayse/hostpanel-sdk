namespace Hostpanel.Sdk.Packaging.Enums;

/// <summary>A way the SSH key agent files of a plugin package fail the agent check. Values start at 1; 0 is not a rule.</summary>
public enum PluginAgentRule
{
    /// <summary>The manifest names an <c>sshAgentProfile</c> but the package has no profile file.</summary>
    ProfileMissing = 1,

    /// <summary>
    /// The profile file cannot be used: it is over the size cap, is not a regular file, cannot be read, is not text in the
    /// agent's configuration format, or has a section, key or value the format does not allow.
    /// </summary>
    ProfileInvalid = 2,

    /// <summary>A file of the agent files, or a folder on the way to one, is a symbolic link or junction.</summary>
    LinkNotAllowed = 3,

    /// <summary>The profile sets a key the agent does not define.</summary>
    KeyNotAllowed = 4,

    /// <summary>The profile's <c>provider</c> is missing or is not the id the manifest names in <c>sshAgentProfile</c>.</summary>
    ProviderMismatch = 5,

    /// <summary>The profile sets a <c>binding</c>: the host fills it in per server, a package never does.</summary>
    BindingNotEmpty = 6,

    /// <summary>The profile's <c>account_check_command</c> or <c>helper_dir</c> is not inside the agent's standard helper folder.</summary>
    PathOutsideHelperDirectory = 7,

    /// <summary>A helper has a name that is not allowed, is not a regular file, is over the size cap or cannot be read.</summary>
    HelperInvalid = 8,

    /// <summary>The package carries more helpers than <see cref="Hostpanel.Sdk.Plugins.Manifest.PluginSshAgentLayout.MaxHelperCount"/>.</summary>
    TooManyHelpers = 9,
}
