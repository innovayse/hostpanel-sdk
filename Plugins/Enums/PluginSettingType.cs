namespace Innovayse.SDK.Plugins.Enums;

/// <summary>
/// The input kind of one typed plugin setting, written in the <c>type</c> member of a <c>settings</c> entry
/// of <c>plugin.json</c> (camelCase, for example <c>url</c>). Values start at 1; 0 is not a kind.
/// </summary>
public enum PluginSettingType
{
    /// <summary>Single-line plain text. The kind every legacy <c>fields</c> entry of an unknown type maps to.</summary>
    Text = 1,

    /// <summary>An absolute web address.</summary>
    Url = 2,

    /// <summary>One of the fixed options of the setting.</summary>
    Select = 3,

    /// <summary>A credential: stored encrypted and never returned by a read.</summary>
    Secret = 4,

    /// <summary>A number.</summary>
    Number = 5,

    /// <summary>An on/off switch stored as <c>true</c> or <c>false</c>.</summary>
    Boolean = 6,

    /// <summary>Multi-line text.</summary>
    Textarea = 7,
}
