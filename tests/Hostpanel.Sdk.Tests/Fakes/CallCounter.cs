namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>Counts how many times each operation of a scripted plugin was called, by method name.</summary>
internal sealed class CallCounter
{
    /// <summary>How many times each operation was called, by method name.</summary>
    private readonly Dictionary<string, int> _calls = new(StringComparer.Ordinal);

    /// <summary>Counts one call of an operation.</summary>
    /// <param name="operation">The method name.</param>
    /// <returns>The call number of this call, starting at 1.</returns>
    internal int Count(string operation)
    {
        var next = _calls.GetValueOrDefault(operation) + 1;
        _calls[operation] = next;
        return next;
    }

    /// <summary>Gets how many times an operation was called.</summary>
    /// <param name="operation">The method name, for example <c>CreateAccountAsync</c>.</param>
    /// <returns>The number of calls, zero when it was never called.</returns>
    internal int CallsOf(string operation)
    {
        return _calls.GetValueOrDefault(operation);
    }
}
