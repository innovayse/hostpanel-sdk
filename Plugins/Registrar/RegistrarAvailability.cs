namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Whether a domain name can be registered now.</summary>
/// <param name="IsAvailable"><see langword="true"/> when the registrar would accept a registration of the name; <see langword="false"/> when it is taken, reserved or not offered.</param>
public sealed record RegistrarAvailability(bool IsAvailable);
