namespace Innovayse.SDK.Plugins.Endpoints;

/// <summary>
/// Who is calling, as much as a plugin may know. The references are opaque and stable for one plugin: the same client
/// gets the same <see cref="ClientRef"/> on every call to that plugin, a different one on every other plugin, and no
/// plugin can turn one into a host id. There is no access token or credential of any kind.
/// </summary>
public sealed record PluginCaller
{
    /// <summary>Backing field of <see cref="Permissions"/>; empty unless the host lists some.</summary>
    private readonly IReadOnlyList<string> _permissions = [];

    /// <summary>
    /// Gets the opaque reference of the client the call acts for, or <see langword="null"/> when the call is not about
    /// a client (an admin route outside any client context).
    /// </summary>
    public string? ClientRef { get; init; }

    /// <summary>Gets the opaque reference of the signed-in user or member making the call.</summary>
    public required string UserRef { get; init; }

    /// <summary>
    /// Gets the names of the client permissions the caller holds in the client it acts for. Empty on an admin call.
    /// The host has already checked the route's own permission before the plugin runs.
    /// </summary>
    public IReadOnlyList<string> Permissions
    {
        get => _permissions;
        init => _permissions = value ?? [];
    }

    /// <summary>Tells whether the caller holds a client permission.</summary>
    /// <param name="permission">The permission name, for example <c>ManageInvoices</c>.</param>
    /// <returns><see langword="true"/> when the caller holds it.</returns>
    public bool HasPermission(string permission)
    {
        return _permissions.Contains(permission, StringComparer.Ordinal);
    }
}
