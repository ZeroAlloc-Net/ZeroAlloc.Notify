using System.Collections.Generic;

namespace ZeroAlloc.Notify.Generator.Models;

internal sealed record NotifyClassModel(
    string? Namespace,
    string TypeName,
    bool NotifyPropertyChanged,
    bool NotifyPropertyChanging,
    bool NotifyCollectionChanged,
    bool NotifyDataErrorInfo,
    bool ClassLevelSequential,
    IReadOnlyList<ObservableFieldModel> Fields);
