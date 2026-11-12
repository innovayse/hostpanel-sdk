using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.StoredCredentials;

/// <summary>Input for <see cref="IStoredCredentialPlugin.EnsureCustomerAsync"/>.</summary>
/// <param name="ClientReference">The host's stable reference for the client, opaque to the plugin. Asking twice with the same reference must answer the same customer.</param>
/// <param name="Email">The client's e-mail address, when the gateway wants one on the customer record.</param>
/// <param name="Name">The client's display name, when the gateway wants one on the customer record.</param>
public sealed partial record CredentialCustomerContext(string ClientReference, [property: SecretMember] string? Email, [property: SecretMember] string? Name);
