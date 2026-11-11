namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// Why a domain registrar refused an operation of <see cref="Registrar.IRegistrarOperationsPlugin"/>, in the host's terms.
/// The host turns the value into a sentence in the caller's language, so a plugin never returns the registrar's own text, a
/// domain or a contact detail to describe a refusal. The values start at 1 and 0 is not valid. New values are only ever
/// appended.
/// </summary>
public enum RegistrarRefusalReason
{
    /// <summary>The domain cannot be registered: it is taken, reserved or not offered by this registrar.</summary>
    DomainUnavailable = 1,

    /// <summary>The registrar does not sell or manage the top level domain.</summary>
    TldNotSupported = 2,

    /// <summary>The authorization (EPP) code of a transfer was rejected.</summary>
    EppCodeInvalid = 3,

    /// <summary>The domain cannot be transferred now, for example because it is locked or was registered or transferred recently.</summary>
    TransferNotAllowed = 4,

    /// <summary>The contact details were rejected, for example a missing or malformed field.</summary>
    ContactInvalid = 5,

    /// <summary>A nameserver was rejected, for example a host name the registrar cannot resolve.</summary>
    NameserverInvalid = 6,

    /// <summary>A DNS record was rejected, for example a value that does not fit its type.</summary>
    DnsRecordInvalid = 7,

    /// <summary>The registrar account has too little balance to pay for the operation.</summary>
    InsufficientFunds = 8,

    /// <summary>The credentials in the connection are valid but not allowed to do this.</summary>
    AccessDenied = 9,

    /// <summary>The registrar refused for a reason none of the other values describes.</summary>
    Other = 10,
}
