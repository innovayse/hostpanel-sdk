namespace Hostpanel.Sdk.Plugins.Diagnostics;

/// <summary>
/// The value a result carries, seen without its type, so a contract helper can check that a read or a list that succeeded
/// really answered one. Internal: a plugin never implements or sees it.
/// </summary>
internal interface IValueAnswer
{
    /// <summary>Gets the value of the result, or <see langword="null"/> when it has none.</summary>
    object? Value { get; }
}
