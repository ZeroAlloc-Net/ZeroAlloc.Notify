# Performance

## Overview

ZeroAlloc.Notify is designed to minimize allocations on property and collection change notifications. The Roslyn source generator emits the event plumbing and the async setters at compile time, so there is no reflection and no runtime code generation.

Where a setter allocates:

- **No subscribers: 0 B.** The generated setter checks for subscribers before it creates the event args, so a change nobody listens to allocates nothing, for reference-type and value-type properties alike.
- **With subscribers:** each raised event allocates its args object, 40 B on 64-bit. For a value-type property the old and new value are also boxed into the args' `object?` properties, 24 B each for an `int`. A setter that raises both `PropertyChanging` and `PropertyChanged` with subscribers on both pays this once per event.
- **Dispatch:** sequential dispatch, and parallel dispatch on .NET 10, add nothing when the handlers complete synchronously. Parallel dispatch on .NET 8 and netstandard currently adds about 136 B per raise inside ZeroAlloc.AsyncEvents, tracked in [ZeroAlloc.AsyncEvents#229](https://github.com/ZeroAlloc-Net/ZeroAlloc.AsyncEvents/issues/229).

The args are a `sealed class` with `object?` values. An allocation-free args shape changes the public event signature, so it is planned for the next major version, see [#237](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/issues/237).

## Benchmark Results

### Property Notifications — 4-way comparison

<!-- BENCH:START -->
_Last refreshed: 2026-10-11_

.NET 10.0.12, i9-12900HK, BenchmarkDotNet v0.15.8. All four libraries are configured with 5 attached handlers to mirror a realistic MVVM scenario.

| Library | Time | Allocated | Async support |
|---|---:|---:|:---:|
| Manual `INotifyPropertyChanged` (baseline) | 35.2 ns | 24 B | ❌ |
| PropertyChanged.Fody | **29.0 ns** | **0 B** | ❌ |
| CommunityToolkit.Mvvm | 50.2 ns | 0 B | ❌ |
| **ZeroAlloc.Notify** | **112.8 ns** | **40 B** | ✅ |

**Honest framing**: ZA.Notify is the slowest of the four and allocates. The 40 B is the `AsyncPropertyChangedEventArgs` object handed to the handlers, much like the `PropertyChangedEventArgs` the manual INPC baseline allocates. The view model also declares `PropertyChanging`, which has no subscribers here, so its args are never created. The other three are pure sync and can't `await` propagation.

**ZA.Notify is the only library here that supports async handlers.** The trade-off is the cost of that capability. For pure-sync view models that never need to `await` a handler (the vast majority of XAML/Avalonia/WinUI scenarios), **Fody is the right choice** — it's fastest and 0 B. For scenarios where the setter needs to wait for async work to complete before returning (e.g., async validation before notifying observers; coordinated async state transitions), ZA.Notify is the only library that supports it.

**Per-iteration scale**: at 112.8 ns / 40 B per setter call, ZA.Notify can run **~8.8M property changes per second per thread**. Most MVVM workloads have property change rates in the thousands per second, where the difference is invisible.

### Where the allocations come from

Same environment, `NotifyAllocationBenchmarks`. The view model declares both `PropertyChanging` and `PropertyChanged`; "five subscribers" means five `PropertyChanged` handlers.

| Setter | No subscribers | Five subscribers |
|---|---:|---:|
| `string` property | 0 B | 40 B |
| `int` property | 0 B | 88 B |
<!-- BENCH:END -->

The test suite gates these numbers in `tests/ZeroAlloc.Notify.Tests/AllocationTests.cs`, so a regression fails the build.

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
```

Results are output to `BenchmarkDotNet.Artifacts/results/`.

## Comparison with Alternatives

### vs INotifyPropertyChanged (Standard INPC)

- **Allocation**: both allocate an args object per notification: 40 B for ZeroAlloc.Notify, 24 B for INPC. With no subscribers ZeroAlloc.Notify allocates nothing
- **Speed**: Async path is 3.2x slower than sync INPC baseline — the cost buys full await semantics
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
