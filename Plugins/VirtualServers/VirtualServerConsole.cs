using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// A console session on a virtual server. Whoever opens the address controls the server, so it is a credential: the
/// generated <c>ToString</c> prints it masked, and neither the plugin nor the host logs it.
/// </summary>
/// <param name="Url">The absolute, time limited address of the console (a VNC or web console). Not empty. Secret.</param>
/// <param name="ExpiresAt">When the address stops working, or <see langword="null"/> when the platform does not say.</param>
public sealed partial record VirtualServerConsole([property: SecretMember] string Url, DateTimeOffset? ExpiresAt);
