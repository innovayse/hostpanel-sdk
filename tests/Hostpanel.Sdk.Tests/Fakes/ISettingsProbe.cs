namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>What the tests need from a base class: reading a setting through its protected method.</summary>
public interface ISettingsProbe
{
    /// <summary>Reads a setting through the base class.</summary>
    /// <param name="key">The field key.</param>
    /// <returns>The stored value, or <see langword="null"/>.</returns>
    string? Read(string key);
}
