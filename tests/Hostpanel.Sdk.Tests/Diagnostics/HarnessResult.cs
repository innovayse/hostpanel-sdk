using Microsoft.CodeAnalysis;

namespace Hostpanel.Sdk.Tests.Diagnostics;

/// <summary>What one run of the secret member generator produced.</summary>
/// <param name="Compilation">The sample compilation with the generated source added.</param>
/// <param name="Run">The generator's own result.</param>
/// <param name="Diagnostics">The diagnostics the generator reported.</param>
internal sealed record HarnessResult(Compilation Compilation, GeneratorDriverRunResult Run, IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>Gets the generated source files, with line ends normalised to line feeds.</summary>
    internal IReadOnlyList<string> Sources =>
        Run.GeneratedTrees.Select(tree => tree.GetText().ToString().Replace("\r\n", "\n")).ToList();
}
