using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hostpanel.Sdk.Generators;

/// <summary>
/// For every record with a member marked <c>[SecretMember]</c>, adds the <c>PrintMembers</c> the record's generated
/// <c>ToString</c> calls, so the marked members print as the mask and all the others print as they would anyway. Nobody
/// writes <c>PrintMembers</c> by hand, so nothing can list the members of a record twice or fall behind when one is
/// added. The record, and every type that encloses it, must be <c>partial</c>. A record that derives from another
/// record calls the base record's method first, as the compiler's own does.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SecretMemberGenerator : IIncrementalGenerator
{
    /// <summary>The name of the step that decides about each record, for the tests that check the generator caches its work.</summary>
    public const string RecordStepName = "SecretRecords";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is RecordDeclarationSyntax,
                static (syntaxContext, ct) => SecretRecordModel.Create(syntaxContext, ct))
            .Where(static model => model is not null)
            .WithTrackingName(RecordStepName);

        context.RegisterSourceOutput(models, static (output, model) => Emit(output, model!));
    }

    /// <summary>Adds the generated source of a record, or reports why it cannot be generated.</summary>
    /// <param name="output">Where the source and the diagnostics go.</param>
    /// <param name="model">The decision about one record.</param>
    private static void Emit(SourceProductionContext output, SecretRecordModel model)
    {
        if (model.Diagnostic is not null)
        {
            output.ReportDiagnostic(Diagnostic.Create(model.Diagnostic, model.Location?.ToLocation(), model.MessageArguments));
            return;
        }

        output.AddSource(model.HintName, model.Source!);
    }
}
