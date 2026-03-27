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

Define a partial method to handle property changes. The generator wires it up automatically:

```csharp
[ObservableProperty]
private string firstName = "";

[NotifyPropertyChangedAsync]
partial void OnFirstNameChanged(string oldValue, string newValue);
```

The source generator creates:

```csharp
// Generated
public async ValueTask SetFirstNameAsync(string value)
{
    if (firstName == value) return;
    var oldValue = firstName;
    firstName = value;
    await OnFirstNameChanged(oldValue, value);
    await RaisePropertyChangedAsync(nameof(FirstName), oldValue, value);
}
```

## Validation During Set

Perform validation in your notification method:

```csharp
[ObservableProperty]
private int age;

[NotifyPropertyChangedAsync]
partial void OnAgeChanged(int oldValue, int newValue)
{
    if (newValue < 0 || newValue > 150)
        throw new ArgumentOutOfRangeException(nameof(Age), "Age must be 0-150");
    
    Console.WriteLine($"Age changed from {oldValue} to {newValue}");
}
```

## Computed Properties

Create computed properties that depend on other observable properties:

```csharp
public partial class PersonViewModel
{
    [ObservableProperty]
    private string firstName = "";

    [ObservableProperty]
    private string lastName = "";

    public string FullName => $"{FirstName} {LastName}";

    [NotifyPropertyChangedAsync]
    partial void OnFirstNameChanged(string oldValue, string newValue)
    {
        await RaisePropertyChangedAsync(nameof(FullName), null, FullName);
    }

    [NotifyPropertyChangedAsync]
    partial void OnLastNameChanged(string oldValue, string newValue)
    {
        await RaisePropertyChangedAsync(nameof(FullName), null, FullName);
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
[ObservableProperty]
private string email = "";

[NotifyPropertyChangedAsync]
partial void OnEmailChanged(string oldValue, string newValue)
{
    // SetEmailAsync will not raise if oldValue == newValue
    // (automatic early exit in generated code)
}
```

## Type Safety

Properties maintain full type safety. The setter method signature matches the property type:

```csharp
[ObservableProperty]
private List<string> items = new();

[NotifyPropertyChangedAsync]
partial void OnItemsChanged(List<string> oldValue, List<string> newValue);

// Type-safe set
await vm.SetItemsAsync(new List<string> { "item1" });
```

## Advanced: Custom Getter/Setter Logic

For complex scenarios, you can customize property access:

```csharp
private string _name = "";

[ObservableProperty]
private string Name
{
    get => _name.ToUpper();
    set => _name = value;
}

[NotifyPropertyChangedAsync]
partial void OnNameChanged(string oldValue, string newValue);
```

## Nullable Reference Types

Full support for nullable reference types:

```csharp
[ObservableProperty]
private string? description;

[ObservableProperty]
private List<string>? items;

[NotifyPropertyChangedAsync]
partial void OnDescriptionChanged(string? oldValue, string? newValue);
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
