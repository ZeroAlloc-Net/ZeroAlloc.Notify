using System.Collections.Generic;

namespace ZeroAlloc.Notify.Generator.Models;

/// <param name="Identity">The fully qualified type, unique per type; generated code is keyed on it.</param>
/// <param name="HintName">The name of the generated file, from <see cref="Pipeline.HintNames.ForType"/>.</param>
internal sealed record NotifyClassModel(
    string Identity,
    string HintName,
    string? Namespace,
    string TypeName,
    bool NotifyPropertyChanged,
    bool NotifyPropertyChanging,
    bool NotifyCollectionChanged,
    bool NotifyDataErrorInfo,
    bool ClassLevelSequential,
    IReadOnlyList<ObservableFieldModel> Fields);
