namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>The type of a DNS record in the zone a registrar hosts. The values start at 1 and 0 is not valid.</summary>
public enum RegistrarDnsRecordType
{
    /// <summary>An IPv4 address.</summary>
    A = 1,

    /// <summary>An IPv6 address.</summary>
    Aaaa = 2,

    /// <summary>An alias to another name.</summary>
    Cname = 3,

    /// <summary>A mail exchanger; the record has a priority.</summary>
    Mx = 4,

    /// <summary>A text record.</summary>
    Txt = 5,

    /// <summary>A delegation to a nameserver.</summary>
    Ns = 6,

    /// <summary>A service locator; the record has a priority.</summary>
    Srv = 7,
}
