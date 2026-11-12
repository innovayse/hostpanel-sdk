namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// How a test of the connection to a hosting panel, a virtual server platform or a registrar ended. The values start at 1
/// and 0 is not valid. A test is the one
/// operation where "cannot reach" is an answer rather than an exception, because finding that out is its purpose.
/// </summary>
public enum PanelConnectionOutcome
{
    /// <summary>The panel answered and accepted the credentials.</summary>
    Connected = 1,

    /// <summary>The server connection lacks what this panel needs, for example no token. Nothing was sent to the panel.</summary>
    NotConfigured = 2,

    /// <summary>The panel answered and refused the credentials.</summary>
    AuthenticationFailed = 3,

    /// <summary>The panel did not answer: refused the connection, timed out, or answered something that is not the panel's API.</summary>
    Unreachable = 4,
}
