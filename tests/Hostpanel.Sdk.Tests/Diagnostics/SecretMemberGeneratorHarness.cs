using System.Reflection;
using Hostpanel.Sdk.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Hostpanel.Sdk.Tests.Diagnostics;

/// <summary>Runs the secret member generator over pieces of C# and compiles what comes out, for the generator's tests.</summary>
internal static class SecretMemberGeneratorHarness
{
    /// <summary>The usings every sample starts with: the attribute the generator looks for.</summary>
    internal const string Header = "using Hostpanel.Sdk.Plugins.Diagnostics;\n";

    /// <summary>Builds the compilation of the samples, one source file each.</summary>
    /// <param name="sources">The samples, without the usings in <see cref="Header"/>.</param>
    /// <returns>The compilation, before the generator ran.</returns>
    internal static CSharpCompilation Compile(params string[] sources)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));
        return CSharpCompilation.Create(
            "Sample",
            sources.Select(source => CSharpSyntaxTree.ParseText(Header + source)),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    /// <summary>Runs the generator over samples.</summary>
    /// <param name="sources">The samples, one source file each, without the usings in <see cref="Header"/>.</param>
    /// <returns>The compilation with the generated source added, what the generator produced, and the diagnostics of the run.</returns>
    internal static HarnessResult Run(params string[] sources)
    {
        var driver = CSharpGeneratorDriver.Create(new SecretMemberGenerator().AsSourceGenerator())
            .RunGeneratorsAndUpdateCompilation(Compile(sources), out var updated, out var generatorDiagnostics);
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
}
