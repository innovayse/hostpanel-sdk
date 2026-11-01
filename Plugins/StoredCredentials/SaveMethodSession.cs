using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.StoredCredentials;

/// <summary>
/// Result of <see cref="IStoredCredentialPlugin.BeginSaveMethodAsync"/>: the session the payer uses to
/// enter a method. The host records the consent before it calls the plugin and completes the session afterwards.
/// </summary>
/// <param name="SessionId">The gateway-side session id; the host passes it to <see cref="IStoredCredentialPlugin.CompleteSaveMethodAsync"/>.</param>
/// <param name="Mode">How the payer enters the method.</param>
/// <param name="ClientToken">The token the plugin's front end part needs; set for <see cref="SaveMethodMode.EmbeddedToken"/>.</param>
/// <param name="RedirectUrl">The gateway page to send the payer to; set for <see cref="SaveMethodMode.Redirect"/>.</param>
public sealed record SaveMethodSession(string SessionId, SaveMethodMode Mode, string? ClientToken, string? RedirectUrl);
