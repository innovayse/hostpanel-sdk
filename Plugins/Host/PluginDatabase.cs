using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Host;

/// <summary>
/// A database the host holds on a hosting service for a plugin, as <see cref="IPluginHostHostingApi.EnsureDatabaseAsync"/>
/// answers. The password is returned once, when the database is created: the host keeps no readable copy, so the
/// plugin stores it where it keeps its own secrets or hands it on, and never logs it. A later call for the same
/// database answers with a null password. The printed form shows the password as <c>***</c>.
/// </summary>
/// <param name="Id">The host's id of the database, for <see cref="IPluginHostHostingApi.RemoveDatabaseAsync"/>.</param>
/// <param name="Name">The database name.</param>
/// <param name="Username">The database user's name.</param>
/// <param name="Host">The host name to connect to.</param>
/// <param name="Port">The port to connect to.</param>
/// <param name="Password">The user's password on creation, otherwise null.</param>
public sealed partial record PluginDatabase(
    Guid Id,
    string Name,
    string Username,
    string Host,
    int Port,
    [property: SecretMember] string? Password);
