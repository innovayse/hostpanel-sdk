namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// Why a hosting panel or a virtual server platform refused an operation, in the host's terms. The host turns the value
/// into a sentence in the caller's language, so a plugin never returns the other side's text, a user name or a domain to
/// describe a refusal. The values start at 1 and 0 is not valid. New values are only ever appended.
/// </summary>
public enum PanelRefusalReason
{
    /// <summary>The package name is not one the panel knows. For a virtual server platform it is the plan name.</summary>
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

    /// <summary>The image a virtual server is created or rebuilt from is not one the platform knows.</summary>
    ImageUnknown = 8,

    /// <summary>The name of a virtual server is not acceptable to the platform (too long, reserved or badly formed), or a different server already holds it. A retried create of our own name is not a refusal: it is <see cref="PanelOperationOutcome.AlreadyApplied"/>.</summary>
    NameInvalid = 9,
}
