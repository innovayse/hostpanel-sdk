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
public sealed record PluginSshAccess(
    Guid KeyId,
    string Host,
    int Port,
    string Username,
    string? Domain,
    string PrivateKey,
    string? HostKeyFingerprint)
{
    /// <summary>Describes the access without the private key, so logging the value never leaks it.</summary>
    /// <returns>The key id, address, user and domain.</returns>
    public override string ToString()
    {
        return $"{nameof(PluginSshAccess)} {{ KeyId = {KeyId}, Host = {Host}, Port = {Port}, Username = {Username}, Domain = {Domain} }}";
    }
}
