---
id: getting-started
title: Getting Started
slug: /
sidebar_position: 1
---

# Getting Started

## Installation

```bash
dotnet add package ZeroAlloc.Notify
```

The source generator is bundled into the package, so this single `PackageReference` is all you need.

> The standalone `ZeroAlloc.Notify.Generator` package is still published for backwards compatibility with existing direct PackageReferences, but new consumers should reference only `ZeroAlloc.Notify`.

The generator needs the .NET 10 SDK 10.0.100 or later, or Visual Studio 2026 or later. It is built against Roslyn 5.0, and an older compiler skips it with warning CS9057, after which the generated code is missing. Version 2.0.0 was built against Roslyn 5.12 and loads only in the .NET 11 SDK; on .NET 10, use 2.0.1 or later. `ZeroAlloc.Notify` also depends on `ZeroAlloc.AsyncEvents`, whose own generator currently needs SDK 10.0.4xx; an older .NET 10 SDK reports CS9057 for that generator only, and the generated Notify code is unaffected (tracked in ZeroAlloc-Net/.github#77).

## Your First Observable Property

### 1. Define a ViewModel

```csharp
using ZeroAlloc.Notify;

[NotifyPropertyChangedAsync]
public partial class UserViewModel
{
    [ObservableProperty]
    private string _name = "Anonymous";
}
```

### 2. Subscribe to Changes

```csharp
var vm = new UserViewModel();

// Subscribe to property changes
vm.PropertyChangedAsync += async (args, ct) =>
{
    Console.WriteLine($"{args.PropertyName} changed to {args.NewValue}");
    await Task.Delay(100, ct);
};

// Setting the property triggers handlers
await vm.SetNameAsync("Alice");
// Output: Name changed to Alice
```

## Observable Collections

```csharp
[NotifyCollectionChangedAsync]
public partial class TodoListViewModel
{
    [ObservableProperty]
    private ObservableCollection<string> _todos = new();
}

var vm = new TodoListViewModel();
vm.CollectionChangedAsync += async (args, ct) =>
{
    Console.WriteLine($"Collection action: {args.Action}");
};

await vm.SetTodosAsync(new ObservableCollection<string> { "Task 1" });
```

## Data Validation

```csharp
using System.Collections;
using System.Collections.Concurrent;
using ZeroAlloc.Notify;

[NotifyDataErrorInfoAsync]
public partial class FormViewModel : INotifyDataErrorInfoAsync
{
    private readonly ConcurrentDictionary<string, List<string>> _errors = new();

    [ObservableProperty]
    private string _email = "";

    public bool HasErrors => !_errors.IsEmpty;

    public IEnumerable GetErrors(string? propertyName)
        => propertyName is null
            ? _errors.Values.SelectMany(e => e)
            : _errors.TryGetValue(propertyName, out var list) ? list : Enumerable.Empty<string>();

    private async ValueTask ValidateEmailAsync(string value, CancellationToken ct)
    {
        _errors.Remove(nameof(Email), out _);
        if (string.IsNullOrEmpty(value))
            _errors[nameof(Email)] = new List<string> { "Email is required" };
        else if (!value.Contains('@'))
            _errors[nameof(Email)] = new List<string> { "Invalid email format" };
        await RaiseErrorsChangedAsync(nameof(Email), ct);
    }
}

var vm = new FormViewModel();
vm.PropertyChangedAsync += async (args, ct) =>
{
    if (args.PropertyName == nameof(vm.Email))
        await vm.ValidateEmailAsync(args.NewValue.As<string>()!, ct);
};

await vm.SetEmailAsync("not-an-email");

var hasErrors = vm.HasErrors;
var errors = vm.GetErrors(nameof(vm.Email));
```

## Key Concepts

### Async All the Way

ZeroAlloc.Notify is **async-first**. Even if your handlers are synchronous, the API uses `ValueTask` to support both blocking and non-blocking handlers without allocation.

```csharp
// Synchronous handler — completes immediately
vm.PropertyChangedAsync += async (args, ct) =>
{
    Console.WriteLine("Handler ran");
    return; // No Task allocation needed
};

// Asynchronous handler — suspends and resumes
vm.PropertyChangedAsync += async (args, ct) =>
{
    await Task.Delay(1000, ct);
    Console.WriteLine("Handler resumed");
};
```

### Sequential vs Parallel

By default, handlers run in **parallel** (fire-and-forget):

```csharp
[NotifyPropertyChangedAsync]
[InvokeSequentially] // Enforce sequential handler execution for this class
public partial class MyViewModel
{
    [ObservableProperty]
    private string _name = "";
}
```

### Source Generation

The source generator automatically creates:
- Setter methods (`SetNameAsync`, `SetEmailAsync`)
- Event raiser methods
- Implementation of `INotifyPropertyChangedAsync`, `INotifyCollectionChangedAsync`, etc.

You apply the attributes; the generator creates the rest.

### Nested and Generic Classes

A class nested in another type, or a generic class, gets its members on the class itself. The
generated file reopens every containing type, so each containing type has to be `partial`:

```csharp
public static partial class Orders
{
    [NotifyPropertyChangedAsync]
    public partial class Entry<T>
    {
        [ObservableProperty]
        private T? _value;
    }
}

await new Orders.Entry<int>().SetValueAsync(7);
```

A nested class in a containing type that is not `partial` gets the error
[ZAN001](diagnostics.md#zan001), and nothing is generated for it.

## Next Steps

- [Observable Properties](observable-properties.md) — Advanced property features
- [Async Notifications](async-notifications.md) — Deep dive into async event handling
- [Performance](performance.md) — Benchmarks and optimization strategies
- [Testing](testing.md) — Unit testing observable models
