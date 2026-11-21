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
                var newValue = args.NewValue.As<int>();   // typed read, no unboxing
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

## Reading Old and New Values

`args.OldValue` and `args.NewValue` are `PropertyValue`s. Read them with the property's type:

```csharp
vm.PropertyChangedAsync += (args, ct) =>
{
    if (args.PropertyName == nameof(PersonViewModel.Age))
    {
        int age = args.NewValue.As<int>();               // throws InvalidCastException for another type
        if (args.OldValue.TryGet<int>(out var previous))  // returns false for another type
            Console.WriteLine($"Age {previous} -> {age}");
    }
    return ValueTask.CompletedTask;
};
```

An inline value matches only its exact type: an `int` property does not read as `long`. Reading as
`object` or an interface, or calling `ToObject()`, boxes the value. A `null` old or new value reads
as `null` for every reference type and nullable value type. `ToString()` formats the value, so
string interpolation works as before. It does not box a type that overrides `ToString()`, such as
`int`, `decimal` or `Guid`; an enum or a struct that inherits `ToString()` is boxed for the call. Do not assign a `PropertyValue` to an
`object` variable or compare it with `Assert.Equal(expected, args.NewValue)`: that boxes the
`PropertyValue` itself, not the value. Use `As<T>()`.

## Property Changed Events

Subscribe to changes for a specific property:

```csharp
vm.PropertyChangedAsync += (args, ct) =>
{
    if (args.PropertyName == nameof(PersonViewModel.FirstName))
    {
        Console.WriteLine($"First name: {args.OldValue} → {args.NewValue}");
    }
    return ValueTask.CompletedTask;
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

- **Allocation**: for a reference-type property, or a value type of up to 16 bytes without references such as `int` or `Guid`, 0 B with or without subscribers when the handlers complete synchronously: the args are structs, and such a value type is held inline instead of boxed. A larger struct, or one that contains references, is boxed once per value
- **Dispatch**: Direct static method call, no virtual dispatch
- **Speed**: with five handlers, a change takes about 1.5 times as long as a hand-written `INotifyPropertyChanged`

See [Performance](performance.md) for detailed benchmarks.

## Next Steps

- [Async Notifications](async-notifications.md) — Handle async operations in property changes
- [Validation](validation.md) — Async validation patterns
- [Getting Started](getting-started.md) — Quick tutorial
