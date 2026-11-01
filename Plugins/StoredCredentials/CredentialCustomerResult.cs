namespace Innovayse.SDK.Plugins.StoredCredentials;

/// <summary>Result of <see cref="IStoredCredentialPlugin.EnsureCustomerAsync"/>.</summary>
/// <param name="CustomerRef">The gateway-side customer reference. The host stores it per client and gateway module and passes it to every later call.</param>
public sealed record CredentialCustomerResult(string CustomerRef);
