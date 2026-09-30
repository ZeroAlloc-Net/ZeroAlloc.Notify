using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using ZeroAlloc.Notify.Generator.Models;
using ZeroAlloc.Notify.Generator.Pipeline;
using ZeroAlloc.Notify.Generator.Writers;

namespace ZeroAlloc.Notify.Generator;

[Generator]
public sealed class NotifyGenerator : IIncrementalGenerator
{
    /// <summary>The tracking name of the step that yields one model per class.</summary>
    internal const string ModelsTrackingName = "NotifyModels";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var notifyChanged    = GetModels(context, "ZeroAlloc.Notify.NotifyPropertyChangedAsyncAttribute");
        var notifyChanging   = GetModels(context, "ZeroAlloc.Notify.NotifyPropertyChangingAsyncAttribute");
        var notifyCollection = GetModels(context, "ZeroAlloc.Notify.NotifyCollectionChangedAsyncAttribute");
        var notifyErrors     = GetModels(context, "ZeroAlloc.Notify.NotifyDataErrorInfoAsyncAttribute");

        var all = notifyChanged
            .Collect()
            .Combine(notifyChanging.Collect())
            .Combine(notifyCollection.Collect())
            .Combine(notifyErrors.Collect())
            .SelectMany((tuple, _) =>
            {
                var (((a, b), c), d) = tuple;
                return Resolve(a.AddRange(b).AddRange(c).AddRange(d));
            })
            .WithTrackingName(ModelsTrackingName);

        context.RegisterSourceOutput(all, Emit);

        // ZAN006: [ObservableProperty] fields whose class has no class-level Notify attribute.
        var orphanFields = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "ZeroAlloc.Notify.ObservablePropertyAttribute",
                static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax,
                NotifyParser.CheckObservableField)
            .Where(static d => d is not null)
            .Select(static (d, _) => d!);

        context.RegisterSourceOutput(orphanFields, static (ctx, diagnostic) => ctx.ReportDiagnostic(diagnostic.ToDiagnostic()));
    }

    /// <summary>
    /// One model per class, in discovery order, with the case collisions marked.
    /// </summary>
    /// <remarks>
    /// A class that is generated drops its location here. The location holds the syntax tree,
    /// which is new after any edit to the class's file, so keeping it would rebuild the class's
    /// file on every keystroke in that file. Only a class with a diagnostic needs it, to report
    /// the diagnostic against the current tree.
    /// </remarks>
    private static List<NotifyClassModel> Resolve(ImmutableArray<NotifyClassModel> items)
    {
        var models = Deduplicate(items);
        var skipped = FindCaseCollisions(models);
        for (var i = 0; i < models.Count; i++)
        {
            var m = models[i];
            if (skipped.TryGetValue(m.QualifiedName, out var diagnostic))
                models[i] = m with { Diagnostic = diagnostic };
            else if (m.Diagnostic is null)
                models[i] = m with { Location = null };
        }
        return models;
    }

    // A class with several Notify attributes is found once per attribute. Deduplicate by the
    // qualified name, which tells apart nested and generic types that share a simple name.
    private static List<NotifyClassModel> Deduplicate(ImmutableArray<NotifyClassModel> items)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var models = new List<NotifyClassModel>(items.Length);
        foreach (var m in items)
        {
            if (seen.Add(m.QualifiedName)) models.Add(m);
        }
        return models;
    }

    /// <summary>
    /// Roslyn compares hint names ignoring case, so two classes whose qualified names differ only
    /// in case cannot both get a file. Among the classes that are generated, the one declared first,
    /// by file path and then position, keeps its file; every later one gets ZAN003 instead. The
    /// result maps the qualified name of each later class to its diagnostic.
    /// </summary>
    private static Dictionary<string, DiagnosticInfo> FindCaseCollisions(List<NotifyClassModel> models)
    {
        var generated = models.FindAll(static m => m.Diagnostic is null);
        generated.Sort(static (x, y) =>
        {
            var byLocation = NotifyParser.CompareLocations(x.Location, y.Location);
            return byLocation != 0 ? byLocation : string.CompareOrdinal(x.QualifiedName, y.QualifiedName);
        });

        var first = new Dictionary<string, NotifyClassModel>(StringComparer.OrdinalIgnoreCase);
        var skipped = new Dictionary<string, DiagnosticInfo>(StringComparer.Ordinal);
        foreach (var m in generated)
        {
            if (!first.TryGetValue(m.QualifiedName, out var earlier))
            {
                first.Add(m.QualifiedName, m);
                continue;
            }

            skipped.Add(m.QualifiedName, DiagnosticInfo.Create(
                NotifyDiagnostics.NameDiffersOnlyInCase, m.Location, m.DisplayName, m.HintName, earlier.DisplayName));
        }
        return skipped;
    }

    private static IncrementalValuesProvider<NotifyClassModel> GetModels(
        IncrementalGeneratorInitializationContext context, string attributeFqn)
        => context.SyntaxProvider
            .ForAttributeWithMetadataName(attributeFqn, NotifyParser.IsCandidate, NotifyParser.Parse)
            .Where(m => m is not null)
            .Select((m, _) => m!);

    private static void Emit(SourceProductionContext ctx, NotifyClassModel model)
    {
        if (model.Diagnostic is not null)
        {
            ctx.ReportDiagnostic(model.Diagnostic.ToDiagnostic());
            return;
        }

        ctx.AddSource(model.HintName, NotifyWriter.Write(model));
    }
}
