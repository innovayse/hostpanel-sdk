namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>A size a virtual server can be created on or moved to, as the platform lists it for the admin who builds a product.</summary>
/// <param name="Name">The plan name exactly as the platform knows it; the host sends it back in <see cref="Provisioning.PanelPackage.Name"/>. Not empty, and unique in one listing.</param>
/// <param name="CpuCores">The number of virtual processors, or <see langword="null"/> when the platform does not say.</param>
/// <param name="MemoryBytes">The memory in bytes, or <see langword="null"/> when the platform does not say.</param>
/// <param name="DiskBytes">The disk size in bytes, or <see langword="null"/> when the platform does not say.</param>
public sealed record VirtualServerPlan(string Name, int? CpuCores, long? MemoryBytes, long? DiskBytes);
