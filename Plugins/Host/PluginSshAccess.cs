using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Host;

/// <summary>
/// An SSH identity the host issued to a plugin for one hosting service of the caller: where to connect, as whom, and
/// the private key. The host keeps only the public half; the private key exists in this value alone, so the plugin
/// stores it where it keeps its own secrets or hands it on, and never logs it.
/// </summary>
/// <param name="KeyId">The host's id of the key, for <see cref="IPluginHostApi.RevokeSshAccessAsync"/>.</param>
/// <param name="Host">The host name clients connect to over SSH.</param>
/// <param name="Port">The SSH port.</param>
/// <param name="Username">The hosting account's user name.</param>
/// <param name="Domain">The service's domain, or null.</param>
/// <param name="PrivateKey">The Ed25519 private key in OpenSSH format, unencrypted.</param>
/// <param name="HostKeyFingerprint">
/// The SHA-256 fingerprint the host has pinned for <paramref name="Host"/>, or null when it has none for that address.
/// </param>
public sealed partial record PluginSshAccess(
    Guid KeyId,
    string Host,
    int Port,
    string Username,
    string? Domain,
    [property: SecretMember] string PrivateKey,
    string? HostKeyFingerprint)
{
    /// <summary>
    /// Gets the server's public SSH host key as one line in the <c>known_hosts</c> format without the host name, for
    /// example <c>ssh-ed25519 AAAA</c>, or null when the host holds none for the address. A plugin pins it to trust that
    /// key from the first connection instead of accepting whatever a first scan shows. <see cref="HostKeyFingerprint"/>
    /// is the SHA-256 fingerprint of the same key, so a plugin may check that the two agree; when both are set they name
    /// one key. It is an init property and not a constructor parameter, so every existing constructor call compiles and
    /// reads as before. Since 3.16.0.
    /// </summary>
    public string? HostKey { get; init; }
}
