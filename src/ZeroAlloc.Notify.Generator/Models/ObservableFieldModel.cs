namespace ZeroAlloc.Notify.Generator.Models;

internal sealed record ObservableFieldModel(
    string FieldName,
    string PropertyName,
    string TypeName,
    bool Sequential);
