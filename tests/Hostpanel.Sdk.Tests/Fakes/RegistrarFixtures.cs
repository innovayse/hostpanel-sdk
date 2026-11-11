using Hostpanel.Sdk.Plugins.Registrar;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>The contexts the registrar tests send. The secret values are fixed so a test can look for them in printed output.</summary>
internal static class RegistrarFixtures
{
    /// <summary>The API key every fixture connection carries.</summary>
    public const string ApiKey = "key-secret-101";

    /// <summary>The authorization code of the fixture transfer.</summary>
    public const string EppCode = "epp-secret-102";

    /// <summary>Gets a connection with the secret set.</summary>
    public static RegistrarConnection Connection { get; } = new("reseller", ApiKey, "https://api.registrar.example.test", "192.0.2.20", false);

    /// <summary>Gets a domain the registrar holds.</summary>
    public static RegistrarDomain Domain { get; } = new("example.test", "dom-1");

    /// <summary>Gets a registrant contact.</summary>
    public static RegistrarContact Contact { get; } = new(
        "Ada",
        "Lovelace",
        null,
        "ada@example.test",
        "+1.5550100",
        "1 Example Street",
        null,
        "Example City",
        "Example State",
        "00000",
        "US");

    /// <summary>Gets the domain with the connection.</summary>
    public static RegistrarDomainContext DomainContext { get; } = new(Connection, Domain);

    /// <summary>Gets a registration of a domain the account does not hold yet.</summary>
    public static RegistrarRegisterContext Register { get; } = new(Connection, "example.test", 1, true, true, ["ns1.example.test", "ns2.example.test"], Contact);

    /// <summary>Gets an incoming transfer.</summary>
    public static RegistrarTransferContext Transfer { get; } = new(Connection, "example.test", EppCode, true, Contact);

    /// <summary>Gets a renewal that carries the expiry the host held.</summary>
    public static RegistrarRenewContext Renew { get; } = new(Connection, Domain, 1, DateTimeOffset.UnixEpoch);

    /// <summary>Gets a change of nameservers.</summary>
    public static RegistrarNameserversContext Nameservers { get; } = new(Connection, Domain, ["ns1.example.test", "ns2.example.test"]);

    /// <summary>Gets a switch to on.</summary>
    public static RegistrarFlagContext Flag { get; } = new(Connection, Domain, true);

    /// <summary>Gets a change of the contact.</summary>
    public static RegistrarContactContext ContactChange { get; } = new(Connection, Domain, Contact);

    /// <summary>Gets the removal of a DNS record.</summary>
    public static RegistrarDnsRecordRemovalContext Removal { get; } = new(Connection, Domain, "rec-1");

    /// <summary>Gets a connection test.</summary>
    public static RegistrarTestConnectionContext Test { get; } = new(Connection);
}
