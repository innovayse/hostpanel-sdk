using Microsoft.CodeAnalysis;

namespace Hostpanel.Sdk.Generators;

/// <summary>The diagnostics the secret member generator reports. A record it cannot cover must fail the build, never print a secret.</summary>
internal static class SecretDiagnostics
{
    /// <summary>The category every diagnostic of the generator is reported under.</summary>
    private const string Category = "Hostpanel.Sdk";

    /// <summary>A record with a secret member, or a type enclosing one, is not declared <c>partial</c>, so nothing can be added to it.</summary>
    internal static readonly DiagnosticDescriptor NotPartial = new(
        "HPSDK001",
        "A record with a secret member must be partial",
        "The record '{0}' has a member marked [SecretMember], so it and every type that encloses it must be declared partial: the generator adds the code that masks the member when the record is printed",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
