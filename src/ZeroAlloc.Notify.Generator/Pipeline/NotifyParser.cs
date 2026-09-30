using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    private static readonly string[] NotifyAttributeFqns =
        { NotifyChangedFqn, NotifyChangingFqn, NotifyCollectionFqn, NotifyErrorsFqn };

    /// <summary>
    /// A class or a record class. Non-partial classes and records are kept so that
    /// <see cref="Check"/> can report them; a record struct cannot carry the class-only Notify
    /// attributes, so the compiler already reports it.
    /// </summary>
    public static bool IsCandidate(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax
            || (node is RecordDeclarationSyntax r && !r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword));

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
    /// Why nothing can be generated for <paramref name="type"/>, or null when it can. The type
    /// itself comes first, since fixing a containing type would not help: ZAN005 for a record,
    /// ZAN004 for a class that is not partial, then ZAN002 when it is file-local, and ZAN001 when a
    /// containing type is not partial.
    /// </summary>
    private static DiagnosticInfo? Check(
        INamedTypeSymbol type, string displayName, LocationInfo? location, CancellationToken ct)
    {
        if (type.IsRecord)
            return DiagnosticInfo.Create(NotifyDiagnostics.RecordNotSupported, location, displayName);

        if (!TypeDeclarations.IsPartial(type, ct))
            return DiagnosticInfo.Create(NotifyDiagnostics.ClassNotPartial, location, displayName);

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

    /// <summary>
    /// ZAN006 for an <c>[ObservableProperty]</c> field whose class has no class-level Notify
    /// attribute, or null. The generator reads observable fields only while it parses a class that
    /// carries one, so such a field would silently get no property.
    /// </summary>
    public static DiagnosticInfo? CheckObservableField(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.TargetSymbol is not IFieldSymbol { ContainingType: { } type } field) return null;
        ct.ThrowIfCancellationRequested();

        var attrs = type.GetAttributes();
        foreach (var fqn in NotifyAttributeFqns)
        {
            if (HasAttr(attrs, fqn)) return null;
        }

        // A field attribute targets one variable declarator; report on its name.
        var location = ctx.TargetNode is VariableDeclaratorSyntax v
            ? v.Identifier.GetLocation()
            : ctx.TargetNode.GetLocation();
        return DiagnosticInfo.Create(
            NotifyDiagnostics.ObservablePropertyWithoutNotifyAttribute,
            LocationInfo.From(location),
            field.Name,
            type.ToDisplayString());
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
