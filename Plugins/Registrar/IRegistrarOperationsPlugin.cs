using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// A domain registrar as a plugin: the operations the host needs to sell and manage domains. A plugin opts in by
/// implementing this interface on its registrar class, which already implements <see cref="IRegistrarPlugin"/> (usually
/// through <see cref="Base.RegistrarProviderBase"/>); the marker interface and the base class do not change, so a registrar
/// plugin built on an earlier version still compiles and loads, and simply has none of these operations. The host checks
/// for this interface at run time and the manifest lists <see cref="PluginCapabilities.RegistrarOperations"/>.
/// </summary>
/// <remarks>
/// <para>
/// One plugin serves one registrar account. The account arrives in each call as a <see cref="RegistrarConnection"/> with the
/// credentials the admin saved, so the plugin keeps no state between calls and holds no session.
/// </para>
/// <para>
/// Network. A registrar plugin takes an <see cref="IPanelTransport"/> in its constructor and sends every request on the
/// client it creates; it never builds its own <see cref="HttpClient"/>. The transport is the same one a hosting panel plugin
/// uses: the host's address guard, timeouts and no retry rule apply only to it. The client has no base address, so the
/// plugin builds absolute addresses from <see cref="RegistrarConnection.ApiUrl"/> or its own default.
/// </para>
/// <para>
/// Outcomes. Every operation answers a <see cref="PanelOperationOutcome"/> (the connection test answers a
/// <see cref="PanelConnectionOutcome"/>). A refusal, a domain the registrar does not hold and an operation the registrar
/// does not have are outcomes, never exceptions. A refusal carries a <see cref="RegistrarRefusalReason"/>, not the
/// registrar's text. A plugin throws only when it does not know what happened, for example when the registrar could not be
/// reached or answered something that is not its API; the host then treats the operation as unfinished and retries it,
/// which is safe because of the next paragraph. A purchase or a change is a success only when the registrar did it: a
/// plugin whose registrar is not configured refuses with <see cref="RegistrarRefusalReason.AccessDenied"/> and never
/// answers a success it did not get.
/// </para>
/// <para>
/// Idempotency. The host retries after a lost answer, so a repeat must end in the same state as the first call and answer a
/// success. After the first success a repeat answers: register <see cref="PanelOperationOutcome.AlreadyApplied"/> with the
/// same <see cref="RegistrarOrder.RegistrarRef"/>, because a domain the account already holds is the earlier attempt;
/// transfer the same, for a transfer already under way or complete; renew <c>AlreadyApplied</c> when the registrar's expiry
/// is already past <see cref="RegistrarRenewContext.CurrentExpiresAt"/> plus the years; every switch and every list of
/// nameservers, contact or DNS record set to the state it is already in <c>Succeeded</c> or <c>AlreadyApplied</c>; deleting
/// a DNS record that is gone <c>AlreadyApplied</c>. <see cref="RegistrarContract"/> checks these promises in a plugin's own
/// tests.
/// </para>
/// <para>
/// Secrets. The API key, the authorization (EPP) code and the registrar's own error text are secrets or may carry one. A
/// result never contains one, and neither does a log line or an exception message of the plugin: the records print the key
/// and the code masked, and a plugin must not undo that. The plugin logs what the registrar said and answers a
/// <see cref="RegistrarRefusalReason"/>.
/// </para>
/// <para>
/// Cancellation. Every operation takes a token and honours it; a cancelled call throws <see cref="OperationCanceledException"/>
/// and the host treats it like any unfinished call.
/// </para>
/// </remarks>
public interface IRegistrarOperationsPlugin : IRegistrarPlugin
{
    /// <summary>Checks whether a domain name can be registered now. Read only; safe to repeat.</summary>
    /// <param name="context">The registrar account and the name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the availability, <c>NotSupported</c> or <c>Refused</c> (<see cref="RegistrarRefusalReason.TldNotSupported"/> for a top level domain the registrar does not sell).</returns>
    Task<RegistrarDataResult<RegistrarAvailability>> CheckAvailabilityAsync(RegistrarAvailabilityContext context, CancellationToken ct);

    /// <summary>
    /// Registers a new domain. Idempotent on the domain name: a domain the registrar already holds for the account is
    /// answered <see cref="PanelOperationOutcome.AlreadyApplied"/> with its reference, and is neither bought again nor
    /// changed.
    /// </summary>
    /// <param name="context">The registrar account, the name, the period, the options and the contact.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The order with the reference to keep, or a refusal (<see cref="RegistrarRefusalReason.DomainUnavailable"/>, <see cref="RegistrarRefusalReason.ContactInvalid"/>, <see cref="RegistrarRefusalReason.InsufficientFunds"/> among others).</returns>
    Task<RegistrarDataResult<RegistrarOrder>> RegisterAsync(RegistrarRegisterContext context, CancellationToken ct);

    /// <summary>
    /// Starts an incoming transfer from another registrar. Idempotent on the domain name: a transfer that is already under
    /// way or complete is answered <see cref="PanelOperationOutcome.AlreadyApplied"/> with its reference. A transfer that
    /// the registrar has not finished answers <see cref="RegistrarOrder.RequiresPolling"/> as <see langword="true"/>.
    /// </summary>
    /// <param name="context">The registrar account, the name, the authorization code and the contact.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The order with the reference to keep, or a refusal (<see cref="RegistrarRefusalReason.EppCodeInvalid"/>, <see cref="RegistrarRefusalReason.TransferNotAllowed"/> among others).</returns>
    Task<RegistrarDataResult<RegistrarOrder>> TransferAsync(RegistrarTransferContext context, CancellationToken ct);

    /// <summary>
    /// Renews a domain for more years. Idempotent through <see cref="RegistrarRenewContext.CurrentExpiresAt"/>: a domain
    /// whose expiry is already past that date plus the years is answered <see cref="PanelOperationOutcome.AlreadyApplied"/>
    /// and is not renewed again.
    /// </summary>
    /// <param name="context">The registrar account, the domain, the years and the expiry the host holds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The order with the new expiry when the registrar reports one, <c>NotFound</c> or a refusal.</returns>
    Task<RegistrarDataResult<RegistrarOrder>> RenewAsync(RegistrarRenewContext context, CancellationToken ct);

    /// <summary>
    /// Asks whether a registration or a transfer the registrar accepted but had not finished is now active. Read only; safe
    /// to repeat. The host calls it for an order that answered <see cref="RegistrarOrder.RequiresPolling"/> as
    /// <see langword="true"/>, until it does not.
    /// </summary>
    /// <param name="context">The registrar account and the domain.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the order, <see cref="RegistrarOrder.RequiresPolling"/> still <see langword="true"/> while the registrar works and <see langword="false"/> with the expiry once the domain is active; <c>NotFound</c> or <c>Refused</c>.</returns>
    Task<RegistrarDataResult<RegistrarOrder>> CheckDomainActiveAsync(RegistrarDomainContext context, CancellationToken ct);

    /// <summary>Cancels an incoming transfer that has not completed. Idempotent: a transfer that is already cancelled is a success.</summary>
    /// <param name="context">The registrar account and the domain.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c>, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<RegistrarActionResult> CancelTransferAsync(RegistrarDomainContext context, CancellationToken ct);

    /// <summary>
    /// Prepares an outgoing transfer to another registrar: it unlocks the domain and makes the authorization code available,
    /// which <see cref="GetEppCodeAsync"/> then reads. Idempotent: a domain that is already prepared is a success.
    /// </summary>
    /// <param name="context">The registrar account and the domain.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c>, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<RegistrarActionResult> InitiateOutgoingTransferAsync(RegistrarDomainContext context, CancellationToken ct);

    /// <summary>Reads the authorization (EPP) code of a domain. Read only; safe to repeat. The code is a secret.</summary>
    /// <param name="context">The registrar account and the domain.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the code, <c>NotFound</c>, <c>NotSupported</c> for a registrar that never reveals it, or <c>Refused</c>.</returns>
    Task<RegistrarDataResult<RegistrarEppCode>> GetEppCodeAsync(RegistrarDomainContext context, CancellationToken ct);

    /// <summary>Reads the nameservers a domain delegates to. Read only; safe to repeat.</summary>
    /// <param name="context">The registrar account and the domain.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the nameservers in order, <c>NotFound</c> or <c>Refused</c>.</returns>
    Task<RegistrarDataResult<IReadOnlyList<string>>> GetNameserversAsync(RegistrarDomainContext context, CancellationToken ct);

    /// <summary>Replaces the nameservers of a domain. Idempotent: a domain that already has exactly this list is a success.</summary>
    /// <param name="context">The registrar account, the domain and the complete list.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> or <c>Refused</c> (<see cref="RegistrarRefusalReason.NameserverInvalid"/> for a nameserver the registrar rejects).</returns>
    Task<RegistrarActionResult> SetNameserversAsync(RegistrarNameserversContext context, CancellationToken ct);

    /// <summary>Switches the registrar's automatic renewal of a domain on or off. Idempotent: a domain already in the wanted state is a success.</summary>
    /// <param name="context">The registrar account, the domain and the wanted state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c>, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<RegistrarActionResult> SetAutoRenewAsync(RegistrarFlagContext context, CancellationToken ct);

    /// <summary>Switches WHOIS privacy of a domain on or off. Idempotent: a domain already in the wanted state is a success.</summary>
    /// <param name="context">The registrar account, the domain and the wanted state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c>, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<RegistrarActionResult> SetWhoisPrivacyAsync(RegistrarFlagContext context, CancellationToken ct);

    /// <summary>Switches the registrar lock (the transfer prohibition) of a domain on or off. Idempotent: a domain already in the wanted state is a success.</summary>
    /// <param name="context">The registrar account, the domain and the wanted state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c>, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<RegistrarActionResult> SetRegistrarLockAsync(RegistrarFlagContext context, CancellationToken ct);

    /// <summary>Reads the registrant contact the registrar holds for a domain. Read only; safe to repeat.</summary>
    /// <param name="context">The registrar account and the domain.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the contact, <c>NotFound</c> when the registrar holds no domain or no contact for it, or <c>Refused</c>.</returns>
    Task<RegistrarDataResult<RegistrarContact>> GetContactAsync(RegistrarDomainContext context, CancellationToken ct);

    /// <summary>Replaces the registrant contact of a domain. Idempotent: a domain that already has this contact is a success.</summary>
    /// <param name="context">The registrar account, the domain and the contact.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> or <c>Refused</c> (<see cref="RegistrarRefusalReason.ContactInvalid"/> for a contact the registrar rejects).</returns>
    Task<RegistrarActionResult> SetContactAsync(RegistrarContactContext context, CancellationToken ct);

    /// <summary>Reads every record of the DNS zone the registrar hosts for a domain. Read only; safe to repeat.</summary>
    /// <param name="context">The registrar account and the domain.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the records (an empty list for an empty zone), <c>NotFound</c>, <c>NotSupported</c> for a registrar with no DNS hosting, or <c>Refused</c>.</returns>
    Task<RegistrarDataResult<IReadOnlyList<RegistrarDnsRecord>>> GetDnsRecordsAsync(RegistrarDomainContext context, CancellationToken ct);

    /// <summary>Adds a record to the domain's DNS zone. A record with the same type, host and value that is already there is <see cref="PanelOperationOutcome.AlreadyApplied"/>.</summary>
    /// <param name="context">The registrar account, the domain and the record, without an id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c>, <c>NotSupported</c> or <c>Refused</c> (<see cref="RegistrarRefusalReason.DnsRecordInvalid"/> for a record the registrar rejects).</returns>
    Task<RegistrarActionResult> AddDnsRecordAsync(RegistrarDnsRecordContext context, CancellationToken ct);

    /// <summary>Changes a record of the domain's DNS zone. Idempotent: a record that already has this content is a success.</summary>
    /// <param name="context">The registrar account, the domain and the record, with its id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> for a domain or a record the registrar does not hold, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<RegistrarActionResult> UpdateDnsRecordAsync(RegistrarDnsRecordContext context, CancellationToken ct);

    /// <summary>Removes a record from the domain's DNS zone. Idempotent: a record that is already gone is <see cref="PanelOperationOutcome.AlreadyApplied"/>, never <c>NotFound</c>.</summary>
    /// <param name="context">The registrar account, the domain and the id of the record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<RegistrarActionResult> DeleteDnsRecordAsync(RegistrarDnsRecordRemovalContext context, CancellationToken ct);

    /// <summary>Reads the registrar's price list for the top level domains it sells. Read only; safe to repeat.</summary>
    /// <param name="context">The registrar account.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with one entry per top level domain, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<RegistrarDataResult<IReadOnlyList<RegistrarTldPricing>>> GetTldPricingAsync(RegistrarPricingContext context, CancellationToken ct);

    /// <summary>
    /// Checks that the host can reach the registrar with the saved credentials. Unlike every other operation it does not
    /// throw for a registrar that cannot be reached: that is the answer it exists to give. It changes nothing at the
    /// registrar.
    /// </summary>
    /// <param name="context">The registrar account to test.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The outcome; the version and count are <see langword="null"/> unless the registrar says them (the count is the number of domains).</returns>
    Task<PanelConnectionTestResult> TestConnectionAsync(RegistrarTestConnectionContext context, CancellationToken ct);
}
