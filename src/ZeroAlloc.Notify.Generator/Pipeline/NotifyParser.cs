using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroAlloc.Notify.Generator.Models;

namespace ZeroAlloc.Notify.Generator.Pipeline;

internal static class NotifyParser
{
    private const string ObservablePropFqn   = "ZeroAlloc.Notify.ObservablePropertyAttribute";
    private const string InvokeSeqFqn        = "ZeroAlloc.Notify.InvokeSequentiallyAttribute";
    private const string NotifyChangedFqn    = "ZeroAlloc.Notify.NotifyPropertyChangedAsyncAttribute";
    private const string NotifyChangingFqn   = "ZeroAlloc.Notify.NotifyPropertyChangingAsyncAttribute";
    private const string NotifyCollectionFqn = "ZeroAlloc.Notify.NotifyCollectionChangedAsyncAttribute";
    private const string NotifyErrorsFqn     = "ZeroAlloc.Notify.NotifyDataErrorInfoAsyncAttribute";

    public static bool IsCandidate(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax c && c.Modifiers.Any(m => string.Equals(m.ValueText, "partial", StringComparison.Ordinal));

    public static NotifyClassModel? Parse(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol type) return null;

        var ns = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString();
        var attrs = type.GetAttributes();

        var classSequential  = HasAttr(attrs, InvokeSeqFqn);
        var notifyChanged    = HasAttr(attrs, NotifyChangedFqn);
        var notifyChanging   = HasAttr(attrs, NotifyChangingFqn);
        var notifyCollection = HasAttr(attrs, NotifyCollectionFqn);
        var notifyErrors     = HasAttr(attrs, NotifyErrorsFqn);

        var fields = new List<ObservableFieldModel>();
        foreach (var member in type.GetMembers())
        {
            if (member is not IFieldSymbol f) continue;
            var fieldAttrs = f.GetAttributes();
            if (!HasAttr(fieldAttrs, ObservablePropFqn)) continue;
            var sequential = classSequential || HasAttr(fieldAttrs, InvokeSeqFqn);
            var propName = ToPascalCase(f.Name.TrimStart('_'));
            fields.Add(new ObservableFieldModel(f.Name, propName, f.Type.ToDisplayString(), sequential));
        }

        return new NotifyClassModel(ns, type.Name, notifyChanged, notifyChanging, notifyCollection, notifyErrors, classSequential, fields);
    }

    private static bool HasAttr(System.Collections.Immutable.ImmutableArray<AttributeData> attrs, string fqn)
    {
        foreach (var a in attrs)
        {
            if (string.Equals(a.AttributeClass?.ToDisplayString(), fqn, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static string ToPascalCase(string name)
    {
        if (name.Length == 0) return name;
        var first = char.ToUpperInvariant(name[0]).ToString();
        return first + name.Substring(1);
    }
}
