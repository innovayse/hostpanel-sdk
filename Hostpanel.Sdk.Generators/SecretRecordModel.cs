using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hostpanel.Sdk.Generators;

/// <summary>
/// What the generator decided about one record: the source to add, or the diagnostic to report. It holds only strings, a
/// descriptor and a plain location, so it compares by value and the compiler skips the record when nothing in it changed.
/// </summary>
internal sealed class SecretRecordModel : IEquatable<SecretRecordModel>
{
    /// <summary>The full name of the attribute that marks a secret member.</summary>
    internal const string AttributeName = "Hostpanel.Sdk.Plugins.Diagnostics.SecretMemberAttribute";

    /// <summary>Initializes a new instance of the <see cref="SecretRecordModel"/> class.</summary>
    /// <param name="hintName">The file name of the generated source.</param>
    /// <param name="source">The source to add, or <see langword="null"/> when a diagnostic is reported instead.</param>
    /// <param name="diagnostic">The diagnostic to report, or <see langword="null"/> when the source is added.</param>
    /// <param name="location">Where the diagnostic points.</param>
    /// <param name="messageArguments">The arguments of the diagnostic's message.</param>
    private SecretRecordModel(string hintName, string? source, DiagnosticDescriptor? diagnostic, LocationInfo? location, string[] messageArguments)
    {
        HintName = hintName;
        Source = source;
        Diagnostic = diagnostic;
        Location = location;
        MessageArguments = messageArguments;
    }

    /// <summary>Gets the file name of the generated source.</summary>
    internal string HintName { get; }

    /// <summary>Gets the source to add, or <see langword="null"/> when a diagnostic is reported instead.</summary>
    internal string? Source { get; }

    /// <summary>Gets the diagnostic to report, or <see langword="null"/> when the source is added.</summary>
    internal DiagnosticDescriptor? Diagnostic { get; }

    /// <summary>Gets where the diagnostic points.</summary>
    internal LocationInfo? Location { get; }

    /// <summary>Gets the arguments of the diagnostic's message.</summary>
    internal string[] MessageArguments { get; }

    /// <summary>Decides what to do with a record declaration.</summary>
    /// <param name="context">The syntax node and the semantic model.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The model, or <see langword="null"/> when the record has no member marked as secret, or when this is not the first part
    /// of a record declared in several parts (each part is visited, and only one may generate).
    /// </returns>
    internal static SecretRecordModel? Create(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var declaration = (RecordDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration, ct) is not INamedTypeSymbol type
            || type.DeclaringSyntaxReferences[0].GetSyntax(ct) != declaration)
        {
            return null;
        }

        var members = RecordSourceWriter.PrintedMembers(type);
        if (!members.Any(member => IsSecret(member)))
        {
            return null;
        }

        var name = type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        var hintName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty)
            .Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_') + ".SecretMembers.g.cs";
        var location = LocationInfo.From(declaration.Identifier.GetLocation());

        if (type.BaseType is { IsRecord: true } baseRecord)
        {
            return new SecretRecordModel(hintName, null, SecretDiagnostics.BaseRecord, location, new[] { name, baseRecord.Name });
        }

        if (!AllPartial(type, ct))
        {
            return new SecretRecordModel(hintName, null, SecretDiagnostics.NotPartial, location, new[] { name });
        }

        return new SecretRecordModel(hintName, RecordSourceWriter.Write(type, members, ct), null, null, Array.Empty<string>());
    }

    /// <summary>Tells whether a member is marked as secret.</summary>
    /// <param name="member">A property or field of the record.</param>
    /// <returns><see langword="true"/> when it carries <c>SecretMemberAttribute</c>.</returns>
    internal static bool IsSecret(ISymbol member)
    {
        return member.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == AttributeName);
    }

    /// <inheritdoc/>
    public bool Equals(SecretRecordModel? other)
    {
        return other is not null
            && HintName == other.HintName
            && Source == other.Source
            && ReferenceEquals(Diagnostic, other.Diagnostic)
            && Equals(Location, other.Location)
            && MessageArguments.SequenceEqual(other.MessageArguments);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as SecretRecordModel);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HintName.GetHashCode();
    }

    /// <summary>Tells whether a type and every type that encloses it is declared <c>partial</c> in the part that is visited.</summary>
    /// <param name="type">The record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when code can be added to all of them.</returns>
    private static bool AllPartial(INamedTypeSymbol type, CancellationToken ct)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            var declared = current.DeclaringSyntaxReferences[0].GetSyntax(ct) as TypeDeclarationSyntax;
            if (declared is null || !declared.Modifiers.Any(modifier => modifier.Text == "partial"))
            {
                return false;
            }
        }

        return true;
    }
}
