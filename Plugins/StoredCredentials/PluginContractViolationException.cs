namespace Hostpanel.Sdk.Plugins.StoredCredentials;

/// <summary>
/// Thrown by a contract helper such as <see cref="StoredCredentialContract"/> when a plugin breaks a promise of its
/// interface. It is not tied to a test framework: any runner reports an unhandled exception as a failed test.
/// </summary>
public sealed class PluginContractViolationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="PluginContractViolationException"/> class.</summary>
    /// <param name="message">Which promise was broken and what was observed.</param>
    public PluginContractViolationException(string message)
        : base(message)
    {
    }
}
