namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>A domain the registrar already holds for the host, as the host addresses it.</summary>
/// <param name="Name">The fully qualified domain name, as the host stores it.</param>
/// <param name="RegistrarRef">The registrar's reference for the domain, exactly as the plugin returned it in <see cref="RegistrarOrder.RegistrarRef"/> when the domain was registered or transferred in.</param>
public sealed record RegistrarDomain(string Name, string RegistrarRef);
