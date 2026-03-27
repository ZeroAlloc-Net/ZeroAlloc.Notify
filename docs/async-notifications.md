# Async Notifications

## Overview

ZeroAlloc.Notify's async notifications allow handlers to perform asynchronous work while maintaining strict ordering, cancellation support, and zero allocations.

## Basic Async Handler

```csharp
var vm = new UserViewModel();

vm.PropertyChangedAsync += async (args, cancellationToken) =>
{
    // This handler can await without blocking
    Console.WriteLine($"Property {args.PropertyName} changed");
    await LogChangeAsync(args, cancellationToken);
    Console.WriteLine("Logging complete");
};

// Setting a property triggers and awaits all handlers
await vm.SetNameAsync("Alice");
```

## Async Validation

Perform async validation during property changes:

```csharp
public partial class RegistrationViewModel
{
    [ObservableProperty]
    private string email = "";

    [NotifyPropertyChangedAsync]
    partial void OnEmailChanged(string oldValue, string newValue)
    {
        // Validate email exists in database
    }

    private async ValueTask ValidateEmailAsync(string email, CancellationToken ct)
    {
        var exists = await _userService.EmailExistsAsync(email, ct);
        if (!exists)
            AddError(nameof(Email), "Email not registered");
    }
}

vm.PropertyChangedAsync += async (args, ct) =>
{
    if (args.PropertyName == nameof(RegistrationViewModel.Email))
    {
        await vm.ValidateEmailAsync(args.NewValue, ct);
    }
};

await vm.SetEmailAsync("test@example.com");
```

## Cancellation Token Support

All async handlers receive a `CancellationToken` for graceful cancellation:

```csharp
vm.PropertyChangedAsync += async (args, cancellationToken) =>
{
    try
    {
        await Task.Delay(5000, cancellationToken);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Handler was cancelled");
    }
};

var cts = new CancellationTokenSource();

// Start an operation
var updateTask = vm.SetNameAsync("Alice");

// Cancel after 1 second
await Task.Delay(1000);
cts.Cancel();
```

## Sequential Execution

By default, handlers run in parallel (fire-and-forget). Use `[InvokeSequentially]` to enforce ordering:

```csharp
public partial class ProcessViewModel
{
    [ObservableProperty]
    private string status = "";

    [InvokeSequentially] // Handlers run in subscribe order
    [NotifyPropertyChangedAsync]
    partial void OnStatusChanged(string oldValue, string newValue);
}

// These handlers will execute in order, awaiting each one
vm.PropertyChangedAsync += async (args, ct) =>
{
    Console.WriteLine("Handler 1 starting");
    await Task.Delay(100, ct);
    Console.WriteLine("Handler 1 complete");
};

vm.PropertyChangedAsync += async (args, ct) =>
{
    Console.WriteLine("Handler 2 starting");
    await Task.Delay(100, ct);
    Console.WriteLine("Handler 2 complete");
};

await vm.SetStatusAsync("processing");
// Output:
// Handler 1 starting
// Handler 1 complete
// Handler 2 starting
// Handler 2 complete
```

## Parallel Execution (Default)

Without `[InvokeSequentially]`, handlers run concurrently:

```csharp
[ObservableProperty]
private string status = "";

[NotifyPropertyChangedAsync]  // No InvokeSequentially
partial void OnStatusChanged(string oldValue, string newValue);

// These run in parallel
vm.PropertyChangedAsync += async (args, ct) =>
{
    Console.WriteLine("Handler 1 starting");
    await Task.Delay(100, ct);
    Console.WriteLine("Handler 1 complete");
};

vm.PropertyChangedAsync += async (args, ct) =>
{
    Console.WriteLine("Handler 2 starting");
    await Task.Delay(100, ct);
    Console.WriteLine("Handler 2 complete");
};

await vm.SetStatusAsync("processing");
// Output (concurrent):
// Handler 1 starting
// Handler 2 starting
// Handler 1 complete
// Handler 2 complete
```

## Error Handling

Exceptions in async handlers propagate to the caller:

```csharp
try
{
    vm.PropertyChangedAsync += async (args, ct) =>
    {
        throw new InvalidOperationException("Handler error");
    };

    await vm.SetNameAsync("Alice");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Handler threw: {ex.Message}");
}
```

With sequential handlers, subsequent handlers won't execute if a previous handler throws.

## Task.Delay vs ValueTask Optimization

Use `ValueTask` to avoid allocation when not actually awaiting:

```csharp
// BAD: Allocates Task object every time
vm.PropertyChangedAsync += async (args, ct) =>
{
    return Task.CompletedTask;
};

// GOOD: No allocation for synchronous completion
vm.PropertyChangedAsync += async (args, ct) =>
{
    if (args.PropertyName != "critical") return;
    await LogAsync(args, ct);
};
```

## Integration with External Services

```csharp
public partial class CustomerViewModel
{
    [ObservableProperty]
    private string email = "";

    private readonly ICustomerService _service;

    public CustomerViewModel(ICustomerService service)
    {
        _service = service;
        
        PropertyChangedAsync += OnPropertyChangedAsync;
    }

    private async ValueTask OnPropertyChangedAsync(AsyncPropertyChangedEventArgs args, CancellationToken ct)
    {
        if (args.PropertyName == nameof(Email))
        {
            var isValid = await _service.ValidateEmailAsync(args.NewValue, ct);
            if (!isValid)
                AddError(nameof(Email), "Email is invalid");
        }
    }
}
```

## Resource Cleanup

Use `try/finally` for guaranteed cleanup:

```csharp
vm.PropertyChangedAsync += async (args, ct) =>
{
    var resource = await AcquireResourceAsync(ct);
    try
    {
        await resource.ProcessAsync(args, ct);
    }
    finally
    {
        await resource.DisposeAsync();
    }
};
```

## Performance Notes

- **ValueTask**: Always used internally; no allocation for sync completion
- **Parallel handlers**: Default for maximum throughput
- **Sequential handlers**: Use only when ordering is critical
- **Cancellation**: Zero overhead if never cancelled

See [Performance](performance.md) for benchmarks.

## Next Steps

- [Observable Properties](observable-properties.md) — Property definition patterns
- [Collection Changes](collection-changes.md) — Async collection events
- [Advanced Patterns](advanced-patterns.md) — Scoped bindings and complex scenarios
