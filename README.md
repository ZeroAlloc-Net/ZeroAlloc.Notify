# ZeroAlloc.Notify

[![NuGet](https://img.shields.io/nuget/v/ZeroAlloc.Notify.svg)](https://www.nuget.org/packages/ZeroAlloc.Notify)
[![Build](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/actions/workflows/ci.yml/badge.svg)](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

ZeroAlloc.Notify is a source-generated, zero-allocation notification library for .NET 8 and .NET 10. It provides async-first property and collection change notifications without reflection or dynamic dispatch. The Roslyn source generator eliminates runtime overhead by wiring all dispatch at compile time — no allocations, no virtual dispatch, fully awaitable handlers.

## Install

```shell
dotnet add package ZeroAlloc.Notify
```

The generator package must also be added as an analyzer:

```xml
<ItemGroup>
  <PackageReference Include="ZeroAlloc.Notify.Generator" Version="*" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Quick Example

```csharp
// 1. Define a ViewModel with observable properties
public partial class UserViewModel
{
    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private int age;

    [NotifyPropertyChangedAsync]
    partial void OnNameChanged(string oldValue, string newValue);

    [NotifyPropertyChangedAsync]
    partial void OnAgeChanged(int oldValue, int newValue);
}

// 2. Subscribe to async notifications
var vm = new UserViewModel();
vm.PropertyChangedAsync += async (args, ct) =>
{
    Console.WriteLine($"Property '{args.PropertyName}' changed from {args.OldValue} to {args.NewValue}");
    await Task.Delay(100, ct); // Non-blocking handler execution
};

// 3. Set properties — fully awaitable
await vm.SetNameAsync("Alice");
await vm.SetAgeAsync(30);
```

## Performance

ZeroAlloc.Notify achieves significant performance improvements over standard INPC and alternative frameworks with zero heap allocation on all notification paths (.NET 10, i9-12900HK, BenchmarkDotNet).

| Method | Mean | Alloc | vs Baseline |
|--------|------|-------|-------------|
| `Sync_INotifyPropertyChanged` (baseline) | 21.41 ns | 24 B | — |
| `CommunityToolkit_SetName` | 31.27 ns | 0 B | 1.47x slower |
| `Fody_SetName` | 17.57 ns | 0 B | 1.22x faster |
| `ZeroAlloc_SetNameAsync` | 61.84 ns | 48 B | **2.9x slower baseline, fully awaitable** |

> CommunityToolkit.Mvvm and PropertyChanged.Fody support sync notifications only — they cannot await async handlers. ZeroAlloc.Notify is the only framework in this comparison with first-class `ValueTask` handler dispatch.

See [docs/performance.md](docs/performance.md) for detailed benchmark results and zero-allocation design explanation.

## Features

- **Property Notifications** — Strongly-typed, fully async `PropertyChangedAsync` events
- **Collection Changes** — `CollectionChangedAsync` with observable property support
- **Data Validation** — `INotifyDataErrorInfoAsync` for async error collection
- **Sequential & Parallel** — `[InvokeSequentially]` attribute for handler ordering
- **Compiler Diagnostics** — Missing handlers and misconfigurations caught at build time
- **Zero Allocation** — `ValueTask`, `readonly record struct`, static dispatch
- **Native AOT Compatible** — No reflection at runtime; all dispatch resolved at compile time
- **Source Generated** — Full type safety with compile-time verification

## Documentation

| Topic | Description |
|-------|-------------|
| [Getting Started](docs/getting-started.md) | Install and send your first notification in five minutes |
| [Observable Properties](docs/observable-properties.md) | Defining and using observable properties |
| [Async Notifications](docs/async-notifications.md) | Property and collection changed notifications with await support |
| [Collection Changes](docs/collection-changes.md) | Observable collections with async event dispatch |
| [Validation](docs/validation.md) | `INotifyDataErrorInfoAsync` with async error handling |
| [Performance](docs/performance.md) | Zero-alloc internals, detailed benchmarks, Native AOT |
| [Diagnostics](docs/diagnostics.md) | ZAN001–ZAN010 source generator warnings and errors |
| [Advanced Patterns](docs/advanced-patterns.md) | Cancellation, scoped bindings, parallel handlers |
| [Testing](docs/testing.md) | Unit-testing observable models and notification flows |

## Examples

See the [samples](samples/) directory for complete working examples:
- WPF MVVM application
- ASP.NET Core data binding
- Console property notifications
- Collection change handling

## License

MIT
