namespace Innovayse.SDK.Plugins.StoredCredentials;

/// <summary>Result of <see cref="IStoredCredentialPlugin.CompleteSaveMethodAsync"/>.</summary>
/// <param name="Saved">Whether the payer finished the session and the gateway stored the method. False when the payer abandoned it or the gateway refused the method.</param>
/// <param name="Method">The stored method; set only when <paramref name="Saved"/> is true.</param>
public sealed record SavedMethodResult(bool Saved, SavedMethod? Method);
