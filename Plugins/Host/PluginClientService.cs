using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.Host;

/// <summary>One service of the call's client. The id is the one a slot passes as the <c>serviceId</c> context key.</summary>
/// <param name="Id">The host's own id of this service, one of the caller's own records.</param>
/// <param name="ProductId">The id of the product the service was ordered from.</param>
/// <param name="Status">The lifecycle state.</param>
/// <param name="Domain">The domain the service is for, or <see langword="null"/> when it has none.</param>
/// <param name="CreatedAt">When the service was created.</param>
/// <param name="NextRenewalAt">When it renews next, or <see langword="null"/> when it does not renew.</param>
public sealed record PluginClientService(
    int Id,
    int ProductId,
    PluginServiceStatus Status,
    string? Domain,
    DateTimeOffset CreatedAt,
    DateTimeOffset? NextRenewalAt);
