namespace Innovayse.SDK.Plugins.Identity;

/// <summary>
/// "This client is account <see cref="Id"/> in system <see cref="Provider"/>" — a CRM id, a
/// tenant id, anything the deployment links its clients to. The host stores the pair on the
/// client and keeps it unique per provider.
/// </summary>
/// <param name="Provider">
/// Short lower-case system name the id belongs to, e.g. "crm". Chosen by the plugin, stable forever.
/// </param>
/// <param name="Id">The account's id in that system.</param>
/// <param name="DisplayName">
/// Name to give a client created lazily for this account; null leaves the host to fall back to
/// the principal's name.
/// </param>
/// <param name="Currency">
/// ISO 4217 alpha code to bill a lazily created client in; null means the host's base currency.
/// </param>
public sealed record ExternalAccount(string Provider, string Id, string? DisplayName, string? Currency);
