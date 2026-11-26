namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// Why a value breaks the domain fields of a domain, as <see cref="Manifest.PluginDomainFieldRules.Validate"/> reports it.
/// Values start at 1; 0 is not a reason.
/// </summary>
public enum PluginDomainFieldViolationReason
{
    /// <summary>The key is not one of the declared fields.</summary>
    Unknown = 1,

    /// <summary>A required field has no value, or only white space.</summary>
    Required = 2,

    /// <summary>The value does not match the field's pattern in full.</summary>
    Pattern = 3,

    /// <summary>The value is shorter than the field's minimum length or longer than its maximum.</summary>
    Length = 4,

    /// <summary>The value is none of the options of a select field.</summary>
    Option = 5,

    /// <summary>The value is not in the format of its type: a <c>yyyy-MM-dd</c> date, a number or an absolute <c>http</c> or <c>https</c> web address.</summary>
    Format = 6,
}
