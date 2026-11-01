namespace Innovayse.SDK.Plugins.Host;

/// <summary>
/// Read-only facts the host shares with a plugin about the client of the current call. The instance is bound to that
/// client by the host: no method takes a client id, so a plugin cannot ask for another client's data. On a call without
/// a client the profile is <see langword="null"/> and the lists are empty.
/// </summary>
public interface IPluginHostApi
{
    /// <summary>Reads the profile of the call's client.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The profile, or <see langword="null"/> when the call has no client.</returns>
    Task<PluginClientProfile?> GetProfileAsync(CancellationToken ct);

    /// <summary>Lists the services of the call's client, newest first.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The services; empty when there are none or the call has no client.</returns>
    Task<IReadOnlyList<PluginClientService>> ListServicesAsync(CancellationToken ct);

    /// <summary>Lists the invoices of the call's client, newest first.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The invoices; empty when there are none or the call has no client.</returns>
    Task<IReadOnlyList<PluginClientInvoice>> ListInvoicesAsync(CancellationToken ct);
}
