using ZeroAlloc.Notify.Generator.Pipeline;

namespace ZeroAlloc.Notify.Generator.Models;

/// <summary>
/// A class that gets notification members. Every member compares by value, so the incremental
/// pipeline reports an unchanged class as unchanged and does not rebuild its file.
/// </summary>
/// <param name="QualifiedName">
/// The type's namespace, containing types and arity, unique within the compilation. It keys
/// deduplication and names the generated file.
/// </param>
/// <param name="DisplayName">The type as diagnostics name it, for example <c>N.Outer.Foo&lt;T&gt;</c>.</param>
/// <param name="Location">The type's first declaration, by file path and then position.</param>
/// <param name="Declarations">
/// The headers of the partial declarations the members go into: the containing types, outermost
/// first, then the type itself.
/// </param>
/// <param name="Diagnostic">Why nothing is generated for the type, or null when it is generated.</param>
internal sealed record NotifyClassModel(
    string QualifiedName,
    string DisplayName,
    LocationInfo? Location,
    string? Namespace,
    EquatableArray<string> Declarations,
    bool NotifyPropertyChanged,
    bool NotifyPropertyChanging,
    bool NotifyCollectionChanged,
    bool NotifyDataErrorInfo,
    bool ClassLevelSequential,
    EquatableArray<ObservableFieldModel> Fields,
    DiagnosticInfo? Diagnostic)
{
    public string HintName => HintNames.ForNotify(QualifiedName);
}
