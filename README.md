# ZeroAlloc.Notify

[![NuGet](https://img.shields.io/nuget/v/ZeroAlloc.Notify.svg)](https://www.nuget.org/packages/ZeroAlloc.Notify)
[![Build](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/actions/workflows/ci.yml/badge.svg)](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![AOT](https://img.shields.io/badge/AOT--Compatible-passing-brightgreen)](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/MarcelRoozekrans?style=flat&logo=githubsponsors&color=ea4aaa&label=Sponsor)](https://github.com/sponsors/MarcelRoozekrans)

ZeroAlloc.Notify is a source-generated notification library for .NET 8 and .NET 10. It provides async-first property and collection change notifications without reflection or dynamic dispatch. The Roslyn source generator eliminates runtime overhead by wiring all dispatch at compile time — no virtual dispatch, fully awaitable handlers.

## Install

The source generator is bundled into the main package — a single `PackageReference` is all you need:

```shell
dotnet add package ZeroAlloc.Notify
```

> The standalone `ZeroAlloc.Notify.Generator` package is still published for backwards compatibility with existing direct PackageReferences, but new consumers should reference only `ZeroAlloc.Notify`.

## Quick Example

```csharp
// 1. Define a ViewModel with observable properties
[NotifyPropertyChangedAsync]
public partial class UserViewModel
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private int _age;
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

ZeroAlloc.Notify provides **async-first, fully awaitable handler dispatch** — the only framework in this comparison where `await vm.SetNameAsync(...)` truly awaits all handlers. The async path runs at competitive speed with modest allocation (.NET 10, i9-12900HK, BenchmarkDotNet).

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
- **Async-First** — Fully awaitable `ValueTask` handlers; no fire-and-forget, no callbacks
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
