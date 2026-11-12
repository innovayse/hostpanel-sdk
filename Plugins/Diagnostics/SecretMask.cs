namespace Hostpanel.Sdk.Plugins.Diagnostics;

/// <summary>
/// The text an SDK record prints in place of a secret member. It is the one text a plugin should print in place of a
/// secret too, in its own settings object or in a diagnostic line about a gateway answer, so every mask in a log is the
/// same and a search for it finds all of them. It is a convention for readable logs and not a security boundary: it
/// does not hide a secret that is written to a log or an answer by other means.
/// </summary>
public static class SecretMask
{
    /// <summary>What is printed instead of a secret value.</summary>
    public const string Text = "***";
}
