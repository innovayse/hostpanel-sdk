namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>Which rule a <c>serverModules</c> entry or list breaks. The values start at 1 and 0 is not valid.</summary>
public enum PluginServerModuleRule
{
    /// <summary>The list is missing or has no entry.</summary>
    ListEmpty = 1,

    /// <summary>An entry is <see langword="null"/>, empty or white space.</summary>
    IdEmpty = 2,

    /// <summary>An entry is longer than <see cref="Manifest.PluginServerModules.MaxIdLength"/> characters.</summary>
    IdTooLong = 3,

    /// <summary>An entry holds a character other than an ASCII letter, a digit, a dash, a dot or an underscore.</summary>
    IdBadCharacters = 4,

    /// <summary>An entry repeats an earlier one; ids are compared ignoring case.</summary>
    IdDuplicate = 5,
}
