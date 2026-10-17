using System.Security.Claims;

namespace Innovayse.SDK.Plugins.Identity;
/// <summary>
/// Contract for an identity plugin (<see cref="Enums.PluginType.Identity"/>): answers "which client is
/// this principal acting for, and may it purchase?". When one is loaded and enabled, every
/// client-portal route resolves the acting client through it instead of the host's default rule
/// (the client whose owner subject equals the principal's subject).
/// </summary>
public interface IClientResolver
{
    /// <summary>Resolves the external account and capabilities of a signed-in principal.</summary>
    /// <param name="principal">The authenticated caller with its claims.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The access the principal has, or <see langword="null"/> when the principal acts for no
    /// account at all (the host then answers "no client profile").
    /// </returns>
    Task<ClientAccess?> ResolveAsync(ClaimsPrincipal principal, CancellationToken ct);
}
