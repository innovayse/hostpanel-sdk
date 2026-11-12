using System.Reflection;
using Hostpanel.Sdk.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Hostpanel.Sdk.Tests.Diagnostics;

/// <summary>Runs the secret member generator over a piece of C# and compiles what comes out, for the generator's tests.</summary>
internal static class SecretMemberGeneratorHarness
{
    /// <summary>The usings every sample starts with: the attribute the generator looks for.</summary>
    internal const string Header = "using Hostpanel.Sdk.Plugins.Diagnostics;\n";

    /// <summary>Runs the generator over a sample.</summary>
    /// <param name="source">The sample, without the usings in <see cref="Header"/>.</param>
    /// <returns>The compilation with the generated source added, what the generator produced, and the diagnostics of both.</returns>
    internal static HarnessResult Run(string source)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));
        var compilation = CSharpCompilation.Create(
            "Sample",
            new[] { CSharpSyntaxTree.ParseText(Header + source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new SecretMemberGenerator().AsSourceGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var generatorDiagnostics);
        return new HarnessResult(updated, driver.GetRunResult(), generatorDiagnostics);
    }

    /// <summary>Compiles the result and loads it.</summary>
    /// <param name="result">A run whose output compiles.</param>
    /// <returns>The assembly the sample and the generated source make.</returns>
    internal static Assembly Load(HarnessResult result)
    {
        using var stream = new MemoryStream();
        var emitted = result.Compilation.Emit(stream);
        emitted.Success.Should().BeTrue(string.Join(Environment.NewLine, emitted.Diagnostics));
        return Assembly.Load(stream.ToArray());
    }

    /// <summary>What one run of the generator produced.</summary>
    /// <param name="Compilation">The sample compilation with the generated source added.</param>
    /// <param name="Run">The generator's own result.</param>
    /// <param name="Diagnostics">The diagnostics the generator reported.</param>
    internal sealed record HarnessResult(Compilation Compilation, GeneratorDriverRunResult Run, IReadOnlyList<Diagnostic> Diagnostics)
    {
        /// <summary>Gets the generated source files, with line ends normalised to line feeds.</summary>
        internal IReadOnlyList<string> Sources =>
            Run.GeneratedTrees.Select(tree => tree.GetText().ToString().Replace("\r\n", "\n")).ToList();
    }
}
