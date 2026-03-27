# Collection Changes

## Overview

`INotifyCollectionChangedAsync` provides async-first collection change notifications. Use `[NotifyCollectionChangedAsync]` on any `ObservableCollection<T>` property.

## Basic Setup

```csharp
using System.Collections.ObjectModel;
using ZeroAlloc.Notify;

public partial class TodoListViewModel
{
    [ObservableProperty]
    private ObservableCollection<string> todos = new();

    [NotifyCollectionChangedAsync]
    partial void OnTodosChanged(object? sender, NotifyCollectionChangedEventArgs e);
}
```

## Subscribing to Collection Events

```csharp
var vm = new TodoListViewModel();

vm.CollectionChangedAsync += async (args, ct) =>
{
    Console.WriteLine($"Action: {args.Action}");
    if (args.NewItems != null)
        foreach (var item in args.NewItems)
            Console.WriteLine($"  Added: {item}");
};

await vm.SetTodosAsync(new ObservableCollection<string> { "Buy milk", "Write tests" });
```

## Supported Actions

All `NotifyCollectionChangedAction` values are forwarded:

| Action | Description |
|--------|-------------|
| `Add` | Items added to the collection |
| `Remove` | Items removed |
| `Replace` | Items replaced at an index |
| `Move` | Items moved within the collection |
| `Reset` | Collection cleared or replaced |

## Performance

Collection change events use the same dispatch path as property notifications. `AsyncCollectionChangedEventArgs` is a `readonly struct` passed on the stack, minimizing allocation.

See [Performance](performance.md) for benchmarks.

## Next Steps

- [Async Notifications](async-notifications.md) — Property change patterns
- [Advanced Patterns](advanced-patterns.md) — Parallel and sequential handler dispatch
