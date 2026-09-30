using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        var displayName = type.ToDisplayString();
        var location = FirstLocation(type);
        return new NotifyClassModel(
            HintNames.QualifiedName(type),
            displayName,
            location,
            ns,
            TypeDeclarations.Headers(type),
            notifyChanged, notifyChanging, notifyCollection, notifyErrors, classSequential,
            new EquatableArray<ObservableFieldModel>(fields.ToImmutableArray()),
            Check(type, displayName, location, ct));
    }

    /// <summary>
    /// Why nothing can be generated for <paramref name="type"/>, or null when it can: ZAN002 when
    /// it is file-local, otherwise ZAN001 when a containing type is not partial.
    /// </summary>
    private static DiagnosticInfo? Check(
        INamedTypeSymbol type, string displayName, LocationInfo? location, CancellationToken ct)
    {
        if (TypeDeclarations.IsFileLocalOrNestedInOne(type))
            return DiagnosticInfo.Create(NotifyDiagnostics.FileLocalType, location, displayName);

        var nonPartial = TypeDeclarations.FirstNonPartialContainingType(type, ct);
        return nonPartial is null
            ? null
            : DiagnosticInfo.Create(
                NotifyDiagnostics.ContainingTypeNotPartial, location, displayName, nonPartial.ToDisplayString());
    }

    /// <summary>
    /// The name of the type's first declaration, by file path and then position, so a type
    /// declared in several parts is reported and ordered the same way on every run.
    /// </summary>
    private static LocationInfo? FirstLocation(INamedTypeSymbol type)
    {
        LocationInfo? first = null;
        foreach (var location in type.Locations)
        {
            var info = LocationInfo.From(location);
            if (info is null) continue;
            if (first is null || CompareLocations(info, first) < 0) first = info;
        }
        return first;
    }

    internal static int CompareLocations(LocationInfo? x, LocationInfo? y)
    {
        var byPath = string.CompareOrdinal(x?.FilePath, y?.FilePath);
        return byPath != 0 ? byPath : (x?.Span.Start ?? 0).CompareTo(y?.Span.Start ?? 0);
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
