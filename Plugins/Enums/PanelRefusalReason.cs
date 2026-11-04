namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// Why a hosting panel refused an operation, in the host's terms. The host turns the value into a sentence in the
/// caller's language, so a plugin never returns panel text, a user name or a domain to describe a refusal. The values
/// start at 1 and 0 is not valid.
/// </summary>
public enum PanelRefusalReason
{
    /// <summary>The package name is not one the panel knows.</summary>
    PackageUnknown = 1,

    /// <summary>The user name is not acceptable to the panel (too long, reserved or badly formed).</summary>
    UsernameInvalid = 2,

    /// <summary>The domain is not acceptable to the panel, or it is already hosted there under another account.</summary>
    DomainInvalid = 3,

    /// <summary>The panel's password policy rejected the password.</summary>
    PasswordRejected = 4,

    /// <summary>The panel or the reseller is at a limit, for example the account count or the disk quota.</summary>
    LimitReached = 5,

    /// <summary>The credentials in the server connection are valid but not allowed to do this.</summary>
    AccessDenied = 6,

    /// <summary>The panel refused for a reason none of the other values describes.</summary>
    Other = 7,
}
