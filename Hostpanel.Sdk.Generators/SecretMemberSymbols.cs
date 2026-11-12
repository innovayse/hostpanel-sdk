using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Hostpanel.Sdk.Generators;

/// <summary>What the generator needs to know about the members of a record: which are printed, which are secret and how to name them in code.</summary>
internal static class SecretMemberSymbols
{
    /// <summary>The full name of the attribute that marks a secret member.</summary>
    internal const string AttributeName = "Hostpanel.Sdk.Plugins.Diagnostics.SecretMemberAttribute";

    /// <summary>Tells whether a member is marked as secret.</summary>
    /// <param name="member">A property or field of the record.</param>
    /// <returns><see langword="true"/> when it carries <c>SecretMemberAttribute</c>.</returns>
    internal static bool IsSecret(ISymbol member)
    {
        return member.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == AttributeName);
    }

    /// <summary>
    /// Lists the members the compiler's own generated method would print: the public instance properties and fields declared
    /// in the record, in the order they are declared, except those of a type that cannot be boxed (a ref struct or a pointer),
    /// which the compiler leaves out too. The members of a base record are printed by the base record's own method.
    /// </summary>
    /// <param name="type">The record.</param>
    /// <returns>The members, in declaration order.</returns>
    internal static IReadOnlyList<ISymbol> PrintedMembers(INamedTypeSymbol type)
    {
        return type.GetMembers()
            .Where(member => member.DeclaredAccessibility == Accessibility.Public && !member.IsStatic)
            .Where(member => member is IFieldSymbol { IsConst: false } || member is IPropertySymbol { IsIndexer: false, GetMethod: not null })
            .Where(member => !IsUnprintable(TypeOf(member)))
            .ToList();
    }

    /// <summary>Writes the name of a member as code, with <c>@</c> in front of a name that is a keyword.</summary>
    /// <param name="member">The property or field.</param>
    /// <returns>An identifier that can be written after <c>this.</c>.</returns>
    internal static string CodeName(ISymbol member)
    {
        return SyntaxFacts.GetKeywordKind(member.Name) != SyntaxKind.None ? "@" + member.Name : member.Name;
    }

    /// <summary>Reads the type of a property or field.</summary>
    /// <param name="member">The property or field.</param>
    /// <returns>Its type.</returns>
    internal static ITypeSymbol TypeOf(ISymbol member)
    {
        return member is IPropertySymbol property ? property.Type : ((IFieldSymbol)member).Type;
    }

    /// <summary>Tells whether a type cannot be boxed, so a member of it cannot be passed to a text builder.</summary>
    /// <param name="type">The member's type.</param>
    /// <returns><see langword="true"/> for a ref struct, a pointer or a function pointer.</returns>
    private static bool IsUnprintable(ITypeSymbol type)
    {
        return type.IsRefLikeType || type.TypeKind == TypeKind.Pointer || type.TypeKind == TypeKind.FunctionPointer;
    }
}
