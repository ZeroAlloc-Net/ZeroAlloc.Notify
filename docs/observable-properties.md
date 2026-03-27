# Observable Properties

## Overview

Observable properties are the core building block of ZeroAlloc.Notify. They provide strong typing, zero allocation, and full async notification support.

## Defining Observable Properties

Use the `[ObservableProperty]` attribute on a backing field to create an observable property:

```csharp
public partial class PersonViewModel
{
    [ObservableProperty]
    private string firstName = "";

    [ObservableProperty]
    private string lastName = "";

    [ObservableProperty]
    private int age;
}
```

The source generator automatically creates:
- Public property accessors (`FirstName`, `LastName`, `Age`)
- Private setter methods (`SetFirstNameAsync`, `SetLastNameAsync`, `SetAgeAsync`)

## Property Notification Methods

Apply `[NotifyPropertyChangedAsync]` to the class and `[ObservableProperty]` to backing fields. The generator wires up all dispatch automatically:

```csharp
[NotifyPropertyChangedAsync]
public partial class PersonViewModel
{
    [ObservableProperty]
    private string _firstName = "";
}
```

The source generator creates:

```csharp
// Generated
public async ValueTask SetFirstNameAsync(string value, CancellationToken ct = default)
{
    if (_firstName == value) return;
    var oldValue = _firstName;
    _firstName = value;
    await RaisePropertyChangedAsync(nameof(FirstName), oldValue, value, ct);
}
```

## Validation During Set

Perform validation by subscribing to `PropertyChangedAsync` in the constructor:

```csharp
[NotifyPropertyChangedAsync]
public partial class PersonViewModel
{
    [ObservableProperty]
    private int _age;

    public PersonViewModel()
    {
        PropertyChangedAsync += (args, ct) =>
        {
            if (args.PropertyName == nameof(Age))
            {
                var newValue = (int)args.NewValue!;
                if (newValue < 0 || newValue > 150)
                    throw new ArgumentOutOfRangeException(nameof(Age), "Age must be 0-150");
                Console.WriteLine($"Age changed from {args.OldValue} to {newValue}");
            }
            return ValueTask.CompletedTask;
        };
    }
}
```

## Computed Properties

Create computed properties that depend on other observable properties:

```csharp
[NotifyPropertyChangedAsync]
public partial class PersonViewModel
{
    [ObservableProperty]
    private string _firstName = "";

    [ObservableProperty]
    private string _lastName = "";

    public string FullName => $"{FirstName} {LastName}";

    public PersonViewModel()
    {
        PropertyChangedAsync += async (args, ct) =>
        {
            if (args.PropertyName == nameof(FirstName) || args.PropertyName == nameof(LastName))
                await RaisePropertyChangedAsync(nameof(FullName), null, FullName, ct);
        };
    }
}
```

## Property Changed Events

Subscribe to changes for a specific property:

```csharp
await vm.PropertyChangedAsync += async (args, ct) =>
{
    if (args.PropertyName == nameof(PersonViewModel.FirstName))
    {
        Console.WriteLine($"First name: {args.OldValue} → {args.NewValue}");
    }
};
```

Or use direct typed handlers via the `INotifyPropertyChangedAsync` interface.

## Conditional Notifications

Only notify if the value actually changes (early exit):

```csharp
[NotifyPropertyChangedAsync]
public partial class UserViewModel
{
    [ObservableProperty]
    private string _email = "";
    // SetEmailAsync will not raise if oldValue == newValue
    // (automatic early exit in generated code)
}
```

## Type Safety

Properties maintain full type safety. The setter method signature matches the property type:

```csharp
[NotifyPropertyChangedAsync]
public partial class ItemViewModel
{
    [ObservableProperty]
    private List<string> _items = new();
}

// Type-safe set
await vm.SetItemsAsync(new List<string> { "item1" });
```

## Advanced: Custom Getter/Setter Logic

For complex scenarios, you can customize property access:

```csharp
[NotifyPropertyChangedAsync]
public partial class UserViewModel
{
    private string _nameRaw = "";

    [ObservableProperty]
    private string _name = "";
    // The generator creates SetNameAsync; subscribe to PropertyChangedAsync
    // for any post-set logic.
}
```

## Nullable Reference Types

Full support for nullable reference types:

```csharp
[NotifyPropertyChangedAsync]
public partial class ContentViewModel
{
    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private List<string>? _items;
}
```

## Performance Characteristics

- **Allocation**: 0 bytes
- **Dispatch**: Direct static method call, no virtual dispatch
- **Inlining**: JIT typically inlines property setters
- **Speed**: ~0.8–1.2 ns with 5 handlers

See [Performance](performance.md) for detailed benchmarks.

## Next Steps

- [Async Notifications](async-notifications.md) — Handle async operations in property changes
- [Validation](validation.md) — Async validation patterns
- [Getting Started](getting-started.md) — Quick tutorial
