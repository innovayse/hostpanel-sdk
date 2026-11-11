namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>An operating system image a virtual server can be created or rebuilt from.</summary>
/// <param name="Id">The platform's id of the image, which the host sends back as the image to install. Not empty, and unique in one listing.</param>
/// <param name="Name">The display name, for the admin who picks the image.</param>
public sealed record VirtualServerImage(string Id, string Name);
