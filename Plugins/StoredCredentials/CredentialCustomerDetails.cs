namespace Innovayse.SDK.Plugins.StoredCredentials;

/// <summary>Input for <see cref="IStoredCredentialPlugin.EnsureCustomerAsync"/>.</summary>
/// <param name="ClientReference">The host's stable reference for the client, opaque to the plugin. Asking twice with the same reference must answer the same customer.</param>
/// <param name="Email">The client's e-mail address, when the gateway wants one on the customer record.</param>
/// <param name="Name">The client's display name, when the gateway wants one on the customer record.</param>
public sealed record CredentialCustomerDetails(string ClientReference, string? Email, string? Name);
