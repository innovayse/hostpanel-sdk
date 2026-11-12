using System.Collections;
using System.Reflection;

namespace Hostpanel.Sdk.Tests.Diagnostics;

/// <summary>
/// Builds a sample of any SDK record from its widest public constructor, so a guard test can print every record without a
/// hand kept list of them: a record added later is sampled the day it is declared. Text arguments carry a marker, so the
/// printed form shows which values were masked.
/// </summary>
internal static class RecordSamples
{
    /// <summary>The deepest nesting of records inside records the builder follows before it gives up.</summary>
    private const int MaxDepth = 6;

    /// <summary>The marker that starts every text value the builder invents.</summary>
    internal const string Marker = "sample-value";

    /// <summary>Builds a sample of the record.</summary>
    /// <param name="type">The record type.</param>
    /// <returns>An instance whose text members are <c>sample-value-</c> followed by the member's name.</returns>
    internal static object Create(Type type)
    {
        return Create(type, 0);
    }

    /// <summary>Builds a sample of the record at a nesting depth.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="depth">How many records enclose this one.</param>
    /// <returns>The instance.</returns>
    private static object Create(Type type, int depth)
    {
        if (depth > MaxDepth)
        {
            throw new NotSupportedException($"{type.Name} nests records deeper than {MaxDepth}.");
        }

        var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(candidate => candidate.GetParameters().Length)
            .First();
        var arguments = constructor.GetParameters().Select(parameter => ValueFor(parameter, depth)).ToArray();
        return constructor.Invoke(arguments);
    }

    /// <summary>Invents the value of one constructor parameter.</summary>
    /// <param name="parameter">The parameter.</param>
    /// <param name="depth">How many records enclose the one being built.</param>
    /// <returns>A value the parameter's type accepts.</returns>
    private static object? ValueFor(ParameterInfo parameter, int depth)
    {
        var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        if (type == typeof(string))
        {
            return $"{Marker}-{parameter.Name}";
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0);
        }

        if (type == typeof(Guid))
        {
            return Guid.NewGuid();
        }

        if (type == typeof(DateTimeOffset))
        {
            return DateTimeOffset.UnixEpoch;
        }

        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType()!, 0);
        }

        if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
        {
            return CreateEmptyCollection(type);
        }

        return Create(type, depth + 1);
    }

    /// <summary>Builds an empty list or dictionary for a read only collection parameter.</summary>
    /// <param name="type">The collection type, such as <c>IReadOnlyList&lt;T&gt;</c>.</param>
    /// <returns>An empty collection that implements it.</returns>
    private static object CreateEmptyCollection(Type type)
    {
        var arguments = type.GetGenericArguments();
        return arguments.Length == 1
            ? Array.CreateInstance(arguments[0], 0)
            : Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(arguments))!;
    }
}
