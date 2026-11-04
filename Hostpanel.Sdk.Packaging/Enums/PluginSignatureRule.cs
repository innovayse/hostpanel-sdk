namespace Hostpanel.Sdk.Packaging.Enums;

/// <summary>
/// A way a plugin package fails the signature check. A host maps the value to its own error code and a signing tool
/// prints it. Values start at 1; 0 is not a rule.
/// </summary>
public enum PluginSignatureRule
{
    /// <summary>The package carries no signature file.</summary>
    Unsigned = 1,

    /// <summary>
    /// The signature file cannot be read as a signature (not JSON, wrong shape, a signature that is not 64 bytes, over the
    /// size cap), or the package holds a file name or a link that cannot be signed unambiguously.
    /// </summary>
    Malformed = 2,

    /// <summary>The signature names a key id that is not among the trusted keys.</summary>
    UnknownKey = 3,

    /// <summary>The signature does not match the package's files: a file was changed, added or removed after signing.</summary>
    Invalid = 4,
}
