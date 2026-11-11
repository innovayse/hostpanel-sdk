namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// What a registrar answers to an order for a domain: a registration, a transfer in, a renewal, or a later check on a
/// pending one. The reference is stable, so a retry of the same order answers the same value.
/// </summary>
/// <param name="RegistrarRef">The registrar's reference for the domain or the order. Not empty. The host keeps it and sends it back in <see cref="RegistrarDomain.RegistrarRef"/>.</param>
/// <param name="ExpiresAt">When the registration now ends, or <see langword="null"/> when the registrar does not say (the host then extends the expiry it holds). Always set when the order is complete and the registrar reports it.</param>
/// <param name="RequiresPolling"><see langword="true"/> when the registrar accepted the order but has not finished it, as a transfer in usually is; the host then asks again with <see cref="IRegistrarOperationsPlugin.CheckDomainActiveAsync"/> until it is <see langword="false"/>.</param>
public sealed record RegistrarOrder(string RegistrarRef, DateTimeOffset? ExpiresAt, bool RequiresPolling);
