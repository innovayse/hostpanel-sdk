namespace Innovayse.SDK.Plugins.Host;

/// <summary>The profile of the call's client, as much as a plugin may read. No address, phone or notes.</summary>
/// <param name="Name">The client's display name: the company when there is one, otherwise the person.</param>
/// <param name="Email">The client's email address, or <see langword="null"/> when none is on file.</param>
/// <param name="Currency">The ISO 4217 alpha code the client is billed in, or <see langword="null"/> for the host's base currency.</param>
/// <param name="Language">The client's preferred two-letter language code, or <see langword="null"/> when not set.</param>
public sealed record PluginClientProfile(string Name, string? Email, string? Currency, string? Language);
