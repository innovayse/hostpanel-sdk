namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// The domain operations a set of domain fields applies to, written in the <c>operations</c> member of a
/// <c>domainFields</c> entry of <c>plugin.json</c> as a comma separated list of names, for example
/// <c>"register, transfer"</c>. The values are bit flags; <see cref="None"/> is not an operation.
/// </summary>
[Flags]
public enum PluginDomainOperations
{
    /// <summary>No operation. A set never declares it.</summary>
    None = 0,

    /// <summary>Registering a new domain.</summary>
    Register = 1,

    /// <summary>Transferring a domain in.</summary>
    Transfer = 2,

    /// <summary>Changing the registrant contact of a domain the registrar holds.</summary>
    ContactChange = 4,

    /// <summary>Every operation; what a set that names none applies to.</summary>
    All = Register | Transfer | ContactChange,
}
