using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ZeroAlloc.Notify.Generator.Models;
using ZeroAlloc.Notify.Generator.Pipeline;
using ZeroAlloc.Notify.Generator.Writers;

namespace ZeroAlloc.Notify.Generator;

[Generator]
public sealed class NotifyGenerator : IIncrementalGenerator
{
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
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var result = new List<NotifyClassModel>();
                foreach (var m in a) AddIfNew(seen, result, m);
                foreach (var m in b) AddIfNew(seen, result, m);
                foreach (var m in c) AddIfNew(seen, result, m);
                foreach (var m in d) AddIfNew(seen, result, m);
                return result;
            });

        context.RegisterSourceOutput(all, Emit);
    }

    private static void AddIfNew(HashSet<string> seen, List<NotifyClassModel> result, NotifyClassModel m)
    {
        if (seen.Add($"{m.Namespace}:{m.TypeName}"))
            result.Add(m);
    }

    private static IncrementalValuesProvider<NotifyClassModel> GetModels(
        IncrementalGeneratorInitializationContext context, string attributeFqn)
        => context.SyntaxProvider
            .ForAttributeWithMetadataName(attributeFqn, NotifyParser.IsCandidate, NotifyParser.Parse)
            .Where(m => m is not null)
            .Select((m, _) => m!);

    private static void Emit(SourceProductionContext ctx, NotifyClassModel model)
    {
        var source = NotifyWriter.Write(model);
        var hint = string.IsNullOrEmpty(model.Namespace)
            ? $"{model.TypeName}.Notify.g.cs"
            : $"{model.Namespace}_{model.TypeName}.Notify.g.cs";
        ctx.AddSource(hint, source);
    }
}
