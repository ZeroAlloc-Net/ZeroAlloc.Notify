# Getting Started

## Installation

```bash
dotnet add package ZeroAlloc.Notify
```

Add the generator as an analyzer:

```xml
<ItemGroup>
  <PackageReference Include="ZeroAlloc.Notify.Generator" Version="*" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

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
        await vm.ValidateEmailAsync((string)args.NewValue!, ct);
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

## Next Steps

- [Observable Properties](observable-properties.md) — Advanced property features
- [Async Notifications](async-notifications.md) — Deep dive into async event handling
- [Performance](performance.md) — Benchmarks and optimization strategies
- [Testing](testing.md) — Unit testing observable models
