namespace Innovayse.SDK.Plugins.StoredCredentials;

/// <summary>Input for <see cref="IStoredCredentialPlugin.BeginSaveMethodAsync"/>.</summary>
/// <param name="CustomerRef">The customer reference from <see cref="CredentialCustomerResult"/>.</param>
/// <param name="ReturnUrl">Absolute URL the gateway sends the payer back to in <see cref="Enums.SaveMethodMode.Redirect"/> mode; null when the mode is embedded.</param>
public sealed record SaveMethodContext(string CustomerRef, string? ReturnUrl);
