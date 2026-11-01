namespace Innovayse.SDK.Plugins.StoredCredentials;

/// <summary>Input for <see cref="IStoredCredentialPlugin.BeginSaveMethodAsync"/>.</summary>
/// <param name="CustomerRef">The customer reference from <see cref="CredentialCustomerResult"/>.</param>
/// <param name="ReturnUrl">
/// Absolute URL the gateway sends the payer back to. The host always passes one; a plugin in
/// <see cref="Enums.SaveMethodMode.Redirect"/> mode uses it, and a plugin in <see cref="Enums.SaveMethodMode.EmbeddedToken"/>
/// mode ignores it.
/// </param>
public sealed record SaveMethodContext(string CustomerRef, string ReturnUrl);
