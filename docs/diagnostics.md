# Diagnostics

The ZeroAlloc.Notify source generator emits compiler diagnostics (errors and warnings) when attributes are misused. All diagnostics use the `ZAN` prefix.

## Diagnostic Reference

| ID | Severity | Title | Cause |
|----|----------|-------|-------|
| ZAN001 | Error | Missing partial keyword | Class decorated with a ZeroAlloc.Notify attribute is not `partial` |
| ZAN002 | Error | Missing ObservableProperty field | `[NotifyPropertyChangedAsync]` method does not match any `[ObservableProperty]` field |
| ZAN003 | Error | Duplicate notification attribute | Multiple notify attributes of the same type on the same class |
| ZAN004 | Warning | Handler method not implemented | Partial notify method declared but not implemented — notification will be no-op |
| ZAN005 | Error | Invalid attribute target | Notify attribute placed on a non-method symbol |
| ZAN006 | Warning | InvokeSequentially on parallel event | `[InvokeSequentially]` has no effect when the event is already sequential |
| ZAN007 | Error | Non-void partial method | Notify partial methods must return `void` |
| ZAN008 | Error | Wrong parameter signature | Notify partial method parameters do not match the expected `(T oldValue, T newValue)` pattern |
| ZAN009 | Warning | Nested class not supported | Observable property generation inside nested classes is not supported |
| ZAN010 | Error | Type not accessible | The field type for `[ObservableProperty]` must be at least `internal` |

## Fixing Common Diagnostics

### ZAN001 — Add `partial`

```csharp
// Error
public class MyViewModel
{
    [NotifyPropertyChangedAsync]
    partial void OnNameChanged(string o, string n);
}

// Fix
public partial class MyViewModel { ... }
```

### ZAN002 — Match field name

The partial method name must follow the pattern `On{PropertyName}Changed`:

```csharp
[ObservableProperty]
private string email = "";           // generates property "Email"

[NotifyPropertyChangedAsync]
partial void OnEmailChanged(string oldValue, string newValue); // ✓ matches
```

### ZAN008 — Match parameter types

```csharp
// Error — wrong parameter types
[NotifyPropertyChangedAsync]
partial void OnAgeChanged(object oldValue, object newValue);

// Fix — match the field type exactly
[NotifyPropertyChangedAsync]
partial void OnAgeChanged(int oldValue, int newValue);
```

## Next Steps

- [Getting Started](getting-started.md)
- [Observable Properties](observable-properties.md)
