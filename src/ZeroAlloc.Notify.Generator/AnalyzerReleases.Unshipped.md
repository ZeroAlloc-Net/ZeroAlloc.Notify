; Unshipped analyzer release.
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category         | Severity | Notes
--------|------------------|----------|-------------------------------------------------------
ZAN001  | ZeroAlloc.Notify | Warning  | Containing type of a Notify class is not partial
ZAN002  | ZeroAlloc.Notify | Error    | File-local Notify class is not generated
ZAN003  | ZeroAlloc.Notify | Error    | Class name differs only in case from another Notify class
ZAN004  | ZeroAlloc.Notify | Warning  | Notify class is not partial
ZAN005  | ZeroAlloc.Notify | Warning  | Notify attributes are not supported on records
ZAN006  | ZeroAlloc.Notify | Warning  | [ObservableProperty] field in a class without a Notify attribute
