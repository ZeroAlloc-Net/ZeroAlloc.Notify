# Performance

## Overview

ZeroAlloc.Notify is designed to minimize allocations on property and collection change notifications. The Roslyn source generator emits the event plumbing and the async setters at compile time, so there is no reflection and no runtime code generation.

Where a setter allocates:

- **No subscribers: 0 B.** The generated setter checks for subscribers before it creates the event args, so a change nobody listens to allocates nothing, for reference-type and value-type properties alike.
- **With subscribers:** each raised event allocates its args object, 40 B on 64-bit. For a value-type property the old and new value are also boxed into the args' `object?` properties, 24 B each for an `int`. A setter that raises both `PropertyChanging` and `PropertyChanged` with subscribers on both pays this once per event.
- **Dispatch:** sequential and parallel dispatch add nothing when the handlers complete synchronously, on .NET 8 and .NET 10. Parallel dispatch on .NET 8 used to add about 136 B per raise inside ZeroAlloc.AsyncEvents; [ZeroAlloc.AsyncEvents#229](https://github.com/ZeroAlloc-Net/ZeroAlloc.AsyncEvents/issues/229) fixed that in 1.1.8.

The args are a `sealed class` with `object?` values. An allocation-free args shape changes the public event signature, so it is planned for the next major version, see [#237](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/issues/237).

## Benchmark Results

### Property Notifications — 4-way comparison

<!-- BENCH:START -->
_Last refreshed: 2026-10-21_

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
12th Gen Intel Core i9-12900HK 2.50GHz, 1 CPU, 20 logical and 14 physical cores
.NET SDK 10.0.402
  [Host] : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
```

> Allocations are exact. Speed is shown as same-run ratios; absolute timings are not published.

Measured on an otherwise idle machine, on .NET 10 and .NET 8, out of process, in three launches pinned to two performance cores. Ratio is each row's mean time relative to the hand-written `INotifyPropertyChanged` in the same run, with the lowest and highest of the three launches in brackets. All four libraries have five handlers attached, to mirror a realistic MVVM scenario. CommunityToolkit.Mvvm 8.4.2, PropertyChanged.Fody 4.1.0.

| Library | Ratio, .NET 10 | Ratio, .NET 8 | Allocated | Async support |
|---|---:|---:|---:|:---:|
| Manual `INotifyPropertyChanged` (baseline) | 1.00 | 1.00 | 24 B | ❌ |
| PropertyChanged.Fody | **0.93** (0.91–0.95) | **1.02** (1.02–1.03) | **0 B** | ❌ |
| CommunityToolkit.Mvvm | 1.81 (1.74–1.91) | 1.84 (1.80–1.86) | 0 B | ❌ |
| **ZeroAlloc.Notify** | **1.47** (1.42–1.54) | **1.63** (1.62–1.66) | **40 B** | ✅ |

**Honest framing**: ZA.Notify is slower than a hand-written `INotifyPropertyChanged` and Fody, and it allocates. It is faster than CommunityToolkit.Mvvm here, which allocates nothing. The 40 B is the `AsyncPropertyChangedEventArgs` object handed to the handlers, much like the `PropertyChangedEventArgs` the manual INPC baseline allocates. The view model also declares `PropertyChanging`, which has no subscribers here, so its args are never created. The other three are pure sync and cannot `await` propagation.

**ZA.Notify is the only library here that supports async handlers.** The trade-off is the cost of that capability. For pure-sync view models that never need to `await` a handler (the vast majority of XAML/Avalonia/WinUI scenarios), **Fody is the right choice**: it is as fast as hand-written code and allocates nothing. For scenarios where the setter needs to wait for async work to complete before returning (e.g., async validation before notifying observers; coordinated async state transitions), ZA.Notify is the only library that supports it.

The 2026-10-11 capture put ZA.Notify at 3.2 times the hand-written baseline. This run, three pinned launches on an idle machine, replaces it.

### Where the allocations come from

`NotifyAllocationBenchmarks`, same runs. The view model declares both `PropertyChanging` and `PropertyChanged`; "five subscribers" means five `PropertyChanged` handlers with the default parallel dispatch. Ratio is relative to the `string` setter with no subscribers.

| Setter | No subscribers | Five subscribers | Ratio, .NET 10 | Ratio, .NET 8 |
|---|---:|---:|---|---|
| `string` property | 0 B | 40 B | 1.00 → 2.06 (1.96–2.14) | 1.00 → 2.06 (2.01–2.09) |
| `int` property | 0 B | 88 B | 0.47 (0.45–0.48) → 2.12 (1.92–2.45) | 0.86 (0.81–0.90) → 2.23 (2.15–2.30) |

The `int` property with subscribers pays the 40 B args plus a 24 B box each for the old and the new value.
<!-- BENCH:END -->

The test suite gates these numbers in `tests/ZeroAlloc.Notify.Tests/AllocationTests.cs` on .NET 8 and .NET 10, for sequential and parallel dispatch, so a regression fails the build.

> **Note:** Collection and validation benchmarks are not yet included in the benchmark suite. See `benchmarks/Notify.Benchmarks/` to add more.

## Design

### 1. Subscriber check before the args

Every generated setter and `Raise*Async` method copies the event's handler to a local, checks it for subscribers, and only then creates the args. Simplified, for a property on a class with `PropertyChanged`:

```csharp
public async ValueTask SetCountAsync(int value, CancellationToken ct = default)
{
    if (EqualityComparer<int>.Default.Equals(_count, value)) return;
    var __old = _count;
    _count = value;
    var __changed = _propertyChangedAsync;
    if (__changed.Count != 0)
        await __changed.InvokeAsync(new AsyncPropertyChangedEventArgs(nameof(Count), __old, value), ct);
}
```

### 2. ValueTask

All async paths return `ValueTask`. When there are no subscribers, or every handler completes synchronously, the setter completes synchronously and its async state machine is never moved to the heap.

### 3. Sealed Class Event Args

Event argument types (`AsyncPropertyChangedEventArgs`, etc.) are `sealed class`, created once per raise and passed by reference to every handler:

```csharp
public sealed class AsyncPropertyChangedEventArgs
{
    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
}
```

### 4. Compile-Time Code Generation

The Roslyn source generator analyzes your code at compile time and emits the dispatch code. No reflection happens at runtime.

## Benchmark Environment

- **Framework**: .NET 10.0.12, X64 RyuJIT x86-64-v3
- **Processor**: Intel Core i9-12900HK
- **OS**: Windows 11
- **BenchmarkDotNet Version**: 0.15.8

## Running Benchmarks

To run the benchmark suite yourself:

```bash
cd benchmarks/Notify.Benchmarks
dotnet run -c Release --framework net10.0
dotnet run -c Release --framework net8.0
```

Results are output to `BenchmarkDotNet.Artifacts/results/`.

## Comparison with Alternatives

### vs INotifyPropertyChanged (Standard INPC)

- **Allocation**: both allocate an args object per notification: 40 B for ZeroAlloc.Notify, 24 B for INPC. With no subscribers ZeroAlloc.Notify allocates nothing
- **Speed**: the async path takes 1.47 times as long as the sync INPC baseline on .NET 10 and 1.63 times on .NET 8; the cost buys full await semantics
- **Async support**: INPC has no built-in async support; ZeroAlloc.Notify has first-class `ValueTask` support
- **Full await**: INPC cannot await handlers; ZeroAlloc can

### vs Reactive Extensions (Rx)

- **Startup**: ZeroAlloc compiles all dispatch statically; Rx uses dynamic subscription
- **Allocation**: ZeroAlloc: 0 B with no subscribers, the args object with subscribers; Rx: Often 200+ bytes depending on operators
- **Use case**: ZeroAlloc is optimized for MVVM; Rx is general-purpose event streaming

### vs PropertyChanged.Fody

- **Dispatch**: Fody rewrites IL at build time; ZeroAlloc uses Roslyn source generation
- **Allocation**: Fody is 0 B (sync only); ZeroAlloc async is 40 B with subscribers — the event args object
- **Async support**: Fody has no async handler dispatch; ZeroAlloc has first-class `ValueTask`
- **Full await**: Fody cannot await handlers; ZeroAlloc can

### vs MVVM Toolkit (CommunityToolkit.Mvvm)

- **Async support**: MVVM Toolkit has limited async support; ZeroAlloc has first-class async
- **Allocation pattern**: MVVM Toolkit allocates 0 B for sync notifications; ZeroAlloc async allocates the 40 B event args object when someone subscribes
- **API surface**: MVVM Toolkit is broader (relaycommand, state management); ZeroAlloc is focused on notifications
- **Integration**: ZeroAlloc works standalone; MVVM Toolkit is part of a larger ecosystem

## Native AOT Compatibility

ZeroAlloc.Notify is fully compatible with Native AOT. Since all dispatch is resolved at compile time via the source generator, no reflection is needed at runtime. The package ships net8.0 and net10.0 builds marked `IsAotCompatible`, so the trim and AOT analyzers check the library itself, next to a netstandard2.0 build for older targets:

```bash
dotnet publish -c Release -p PublishAot=true
```

## Memory Profiling

When using ZeroAlloc.Notify in your application, you'll notice:
- **Heap allocations**: none for changes nobody subscribes to; one args object per raised event otherwise
- **GC pressure**: proportional to the number of observed changes, not to all changes
- **Throughput**: Higher sustained throughput when handling millions of notifications

## Optimization Guidelines

To maximize performance:

1. **Use `[InvokeSequentially]`** only when handler order matters — parallel execution is faster
2. **Prefer `ValueTask` over `Task`** for async handlers to avoid boxing
3. **Avoid closures** in handlers; use struct-based state instead
4. **Use `readonly` properties** to signal to the JIT that values don't change
5. **Profile your specific workload** — generic benchmarks may not reflect your use case

## Further Reading

- [Observable Properties](observable-properties.md) — How to define and use properties
- [Async Notifications](async-notifications.md) — Working with async event handlers
- [Testing](testing.md) — Benchmarking your own handlers
