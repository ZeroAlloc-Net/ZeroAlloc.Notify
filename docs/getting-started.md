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
using ZeroAlloc.Notify.Attributes;

public partial class UserViewModel
{
    [ObservableProperty]
    private string name = "Anonymous";

    [NotifyPropertyChangedAsync]
    partial void OnNameChanged(string oldValue, string newValue);
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
public partial class TodoListViewModel
{
    [ObservableProperty]
    private ObservableCollection<string> todos = new();

    [NotifyCollectionChangedAsync]
    partial void OnTodosChanged(object? sender, NotifyCollectionChangedEventArgs e);
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
public partial class FormViewModel : INotifyDataErrorInfoAsync
{
    [ObservableProperty]
    private string email = "";

    [NotifyPropertyChangedAsync]
    partial void OnEmailChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
            AddError(nameof(Email), "Email is required");
        else if (!newValue.Contains("@"))
            AddError(nameof(Email), "Invalid email format");
        else
            RemoveError(nameof(Email));
    }

    [NotifyDataErrorInfoAsync]
    partial void OnErrorsChanged();
}

var vm = new FormViewModel();
var hasErrors = (await vm.GetHasErrorsAsync());
var errors = (await vm.GetErrorsAsync(nameof(FormViewModel.Email))).ToList();
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
[ObservableProperty]
private string name;

[InvokeSequentially] // New: enforce sequential execution
[NotifyPropertyChangedAsync]
partial void OnNameChanged(string oldValue, string newValue);
```

### Source Generation

The source generator automatically creates:
- Setter methods (`SetNameAsync`, `SetEmailAsync`)
- Event raiser methods
- Implementation of `INotifyPropertyChangedAsync`, `INotifyCollectionChangedAsync`, etc.

You write the `partial` method; the generator creates the rest.

## Next Steps

- [Observable Properties](observable-properties.md) — Advanced property features
- [Async Notifications](async-notifications.md) — Deep dive into async event handling
- [Performance](performance.md) — Benchmarks and optimization strategies
- [Testing](testing.md) — Unit testing observable models
