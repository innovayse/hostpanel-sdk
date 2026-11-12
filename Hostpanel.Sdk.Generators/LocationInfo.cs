using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Hostpanel.Sdk.Generators;

/// <summary>
/// A source location kept as plain values. A <see cref="Location"/> holds the syntax tree, which would defeat the
/// generator's cache; this value compares equal when the position is the same, so an unchanged record is not generated again.
/// </summary>
internal sealed class LocationInfo : IEquatable<LocationInfo>
{
    /// <summary>Initializes a new instance of the <see cref="LocationInfo"/> class.</summary>
    /// <param name="path">The path of the source file.</param>
    /// <param name="span">The span of the text in the file.</param>
    /// <param name="lineSpan">The same span as lines and columns.</param>
    private LocationInfo(string path, TextSpan span, LinePositionSpan lineSpan)
    {
        Path = path;
        Span = span;
        LineSpan = lineSpan;
    }

    /// <summary>Gets the path of the source file.</summary>
    internal string Path { get; }

    /// <summary>Gets the span of the text in the file.</summary>
    internal TextSpan Span { get; }

    /// <summary>Gets the span as lines and columns.</summary>
    internal LinePositionSpan LineSpan { get; }

    /// <summary>Copies a location into plain values.</summary>
    /// <param name="location">The location, in source.</param>
    /// <returns>The copy.</returns>
    internal static LocationInfo From(Location location)
    {
        return new LocationInfo(location.SourceTree?.FilePath ?? string.Empty, location.SourceSpan, location.GetLineSpan().Span);
    }

    /// <summary>Builds the location again, for a diagnostic.</summary>
    /// <returns>A location that points at the same text.</returns>
    internal Location ToLocation()
    {
        return Location.Create(Path, Span, LineSpan);
    }

    /// <inheritdoc/>
    public bool Equals(LocationInfo? other)
    {
        return other is not null && Path == other.Path && Span == other.Span && LineSpan == other.LineSpan;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as LocationInfo);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return (Path.GetHashCode() * 31) + Span.GetHashCode();
    }
}
