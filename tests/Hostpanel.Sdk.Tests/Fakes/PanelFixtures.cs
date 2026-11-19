using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>The contexts the provisioning tests send. The secret values are fixed so a test can look for them in printed output.</summary>
internal static class PanelFixtures
{
    /// <summary>The access hash every fixture server carries.</summary>
    public const string AccessHash = "hash-secret-001";

    /// <summary>The API token every fixture server carries.</summary>
    public const string ApiToken = "token-secret-002";

    /// <summary>The initial password of the fixture create.</summary>
    public const string Password = "pw-secret-003";

    /// <summary>The new password of the fixture password change.</summary>
    public const string NewPassword = "pw-secret-004";

    /// <summary>The panel password of the server that signs in with one.</summary>
    public const string ServerPassword = "pw-secret-005";

    /// <summary>The password of the login details every fixture carries.</summary>
    public const string LoginPassword = "pw-secret-006";

    /// <summary>Gets a server connection with both secrets set.</summary>
    public static PanelServerConnection Server { get; } =
        new(7, "node-1", "panel.example.test", "192.0.2.10", 2087, true, "root", AccessHash, ApiToken);

    /// <summary>Gets a server that signs in with a user name and a password and has neither key nor token.</summary>
    public static PanelServerConnection PasswordServer { get; } =
        new(8, "node-2", "panel2.example.test", null, null, true, "root", null, null, Password: ServerPassword);

    /// <summary>Gets the login details of the existing account, with a password.</summary>
    public static PanelLoginDetails LoginDetails { get; } =
        new("acct1", LoginPassword, "example.test", "192.0.2.10", "https://panel.example.test:2083/");

    /// <summary>Gets a service that already has an account.</summary>
    public static PanelService Service { get; } = new(42, "acct1");

    /// <summary>Gets the server and the existing account.</summary>
    public static PanelAccountContext Account { get; } = new(Server, Service);

    /// <summary>Gets a create for a service that has no account yet.</summary>
    public static PanelCreateAccountContext Create { get; } = new(
        Server,
        new PanelService(42, null),
        new PanelClientRef("client-9", "client@example.test", "A Client"),
        new PanelPackage("basic"),
        "example.test",
        "acct1",
        Password);

    /// <summary>Gets a suspend of the existing account.</summary>
    public static PanelSuspendContext Suspend { get; } = new(Server, Service, "Non-payment");

    /// <summary>Gets a termination of the existing account.</summary>
    public static PanelTerminateContext Terminate { get; } = new(Server, Service, "Cancelled");

    /// <summary>Gets a move of the existing account to another package.</summary>
    public static PanelChangePackageContext ChangePackage { get; } = new(Server, Service, new PanelPackage("pro"));

    /// <summary>Gets a password change of the existing account.</summary>
    public static PanelChangePasswordContext ChangePassword { get; } = new(Server, Service, NewPassword);
}
