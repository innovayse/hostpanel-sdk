namespace Innovayse.SDK.Plugins.Enums;

/// <summary>
/// How the payer enters a payment method that is to be stored. The values start at 1 and 0 is not valid: a
/// default-initialised value means the plugin set nothing, and the host refuses it.
/// </summary>
public enum SaveMethodMode
{
    /// <summary>The plugin's own front end part mounts a card field with the client token the session carries.</summary>
    EmbeddedToken = 1,

    /// <summary>The payer is sent to the gateway's page and comes back; the host then completes the session.</summary>
    Redirect = 2,
}
