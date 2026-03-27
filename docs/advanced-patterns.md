# Advanced Patterns

## Cancellation

All `SetXxxAsync` methods accept an optional `CancellationToken`:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await vm.SetNameAsync("Alice", cts.Token);
```

Handlers receive the token and should forward it to any awaited calls:

```csharp
vm.PropertyChangedAsync += async (args, ct) =>
{
    await _service.SyncAsync(args.NewValue, ct); // ct forwarded from SetNameAsync
};
```

## Sequential vs Parallel Dispatch

By default all handlers run in parallel. Apply `[InvokeSequentially]` when handler ordering matters:

```csharp
[InvokeSequentially]
[NotifyPropertyChangedAsync]
partial void OnStatusChanged(string oldValue, string newValue);
```

Sequential execution guarantees handlers run in subscription order, each completing before the next starts. Use it for audit logging, ordered side-effects, or state machines.

Parallel execution is faster — prefer it when handlers are independent.

## Scoped Bindings (DI)

Subscribe inside a scoped service and unsubscribe on disposal:

```csharp
public class MyService : IDisposable
{
    private readonly MyViewModel _vm;

    public MyService(MyViewModel vm)
    {
        _vm = vm;
        _vm.PropertyChangedAsync += OnPropertyChangedAsync;
    }

    private ValueTask OnPropertyChangedAsync(AsyncPropertyChangedEventArgs args, CancellationToken ct)
    {
        return ValueTask.CompletedTask;
    }

    public void Dispose()
        => _vm.PropertyChangedAsync -= OnPropertyChangedAsync;
}
```

## Combining Multiple Notify Interfaces

A single class can implement all four interfaces:

```csharp
[NotifyPropertyChangedAsync]
[NotifyPropertyChangingAsync]
[NotifyCollectionChangedAsync]
[NotifyDataErrorInfoAsync]
public partial class FullViewModel
{
    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private ObservableCollection<string> items = new();
}
```

The generator emits all four interface implementations in a single generated file.

## Error Propagation

Exceptions thrown inside handlers propagate to the `await` call site:

```csharp
try
{
    await vm.SetNameAsync("bad-value");
}
catch (ValidationException ex)
{
    // handler threw; sequential handlers after this did not run
}
```

For parallel handlers, if multiple handlers throw, exceptions are aggregated in an `AggregateException`.

## Next Steps

- [Async Notifications](async-notifications.md)
- [Testing](testing.md)
- [Performance](performance.md)
