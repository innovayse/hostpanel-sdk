using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// What a rebuild or a root password reset answers. A platform that applied the password the host sent has nothing to add,
/// and one that can only generate a password answers it here. <see cref="GeneratedPassword"/> is a secret: the generated
/// <c>ToString</c> prints it masked.
/// </summary>
/// <param name="GeneratedPassword">The root password the platform generated, or <see langword="null"/> when the requested password was applied. Secret.</param>
public sealed partial record VirtualServerRootPassword([property: SecretMember] string? GeneratedPassword);
