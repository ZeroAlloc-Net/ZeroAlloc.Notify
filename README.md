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

ZeroAlloc.Notify provides **async-first, fully awaitable handler dispatch** — the only framework in this comparison where `await vm.SetNameAsync(...)` truly awaits all handlers. Refreshed benchmarks (.NET 10.0.7, i9-12900HK, BenchmarkDotNet v0.15.8, 5 attached handlers):

| Library | Time | Allocated | Async support |
|---|---:|---:|:---:|
| Manual `INotifyPropertyChanged` (baseline) | 33.6 ns | 24 B | ❌ |
| PropertyChanged.Fody | **30.2 ns** | **0 B** | ❌ |
| CommunityToolkit.Mvvm | 55.2 ns | 0 B | ❌ |
| **ZeroAlloc.Notify** | **124.7 ns** | **80 B** | ✅ |

**Honest framing**: ZA.Notify is the slowest of the four and the only one that allocates — the 80 B is the `ValueTask` state machine for fan-out to async handlers. **For pure-sync view models, Fody is the right choice** (fastest, 0 B). ZA.Notify is the only library here that supports async handlers; the trade-off is the cost of that capability. At 124.7 ns / 80 B per setter, it still scales to ~8M property changes per second per thread.

See [docs/performance.md](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/performance.md) for detailed benchmark results and zero-allocation design explanation.

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
| [Getting Started](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/getting-started.md) | Install and send your first notification in five minutes |
| [Observable Properties](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/observable-properties.md) | Defining and using observable properties |
| [Async Notifications](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/async-notifications.md) | Property and collection changed notifications with await support |
| [Collection Changes](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/collection-changes.md) | Observable collections with async event dispatch |
| [Validation](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/validation.md) | `INotifyDataErrorInfoAsync` with async error handling |
| [Performance](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/performance.md) | Zero-alloc internals, detailed benchmarks, Native AOT |
| [Diagnostics](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/diagnostics.md) | ZAN001–ZAN010 source generator warnings and errors |
| [Advanced Patterns](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/advanced-patterns.md) | Cancellation, scoped bindings, parallel handlers |
| [Testing](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/testing.md) | Unit-testing observable models and notification flows |

## Examples

See the [samples](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/tree/main/samples/) directory for complete working examples:
- WPF MVVM application
- ASP.NET Core data binding
- Console property notifications
- Collection change handling

## License

MIT
