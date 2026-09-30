; Unshipped analyzer release.
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category         | Severity | Notes
--------|------------------|----------|-------------------------------------------------------
ZAN001  | ZeroAlloc.Notify | Warning  | Containing type of a Notify class is not partial
ZAN002  | ZeroAlloc.Notify | Error    | File-local Notify class is not generated
ZAN003  | ZeroAlloc.Notify | Error    | Class name differs only in case from another Notify class
