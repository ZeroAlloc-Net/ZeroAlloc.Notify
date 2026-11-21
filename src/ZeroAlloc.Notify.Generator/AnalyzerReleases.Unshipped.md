; Unshipped analyzer release.
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### Changed Rules

Rule ID | New Category     | New Severity | Old Category     | Old Severity | Notes
--------|------------------|--------------|------------------|--------------|-------------------------------------------------------
ZAN001  | ZeroAlloc.Notify | Error        | ZeroAlloc.Notify | Warning      | Containing type of a Notify class is not partial
ZAN004  | ZeroAlloc.Notify | Error        | ZeroAlloc.Notify | Warning      | Notify class is not partial
ZAN005  | ZeroAlloc.Notify | Error        | ZeroAlloc.Notify | Warning      | Notify attributes are not supported on records
ZAN006  | ZeroAlloc.Notify | Error        | ZeroAlloc.Notify | Warning      | [ObservableProperty] field in a class without a Notify attribute
