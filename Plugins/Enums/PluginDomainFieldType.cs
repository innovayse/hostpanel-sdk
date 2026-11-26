namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// The input kind of one domain field a registrar plugin declares, written in the <c>type</c> member of a
/// <c>domainFields</c> field of <c>plugin.json</c> (camelCase, for example <c>textarea</c>). Values start at 1; 0 is not a
/// kind. Every value travels as text in the <c>ExtendedAttributes</c> of a registrar context.
/// </summary>
public enum PluginDomainFieldType
{
    /// <summary>Single-line plain text.</summary>
    Text = 1,

    /// <summary>Multi-line text.</summary>
    Textarea = 2,

    /// <summary>One of the fixed options of the field; the value is the option's <c>value</c>.</summary>
    Select = 3,

    /// <summary>A calendar date written <c>yyyy-MM-dd</c>.</summary>
    Date = 4,

    /// <summary>A number written with an optional sign and an optional decimal point, with no thousands separator.</summary>
    Number = 5,

    /// <summary>An absolute <c>http</c> or <c>https</c> web address.</summary>
    Url = 6,
}
