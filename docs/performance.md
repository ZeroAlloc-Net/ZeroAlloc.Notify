# Performance

## Overview

ZeroAlloc.Notify is designed to minimize allocations on property and collection change notifications. The Roslyn source generator emits the event plumbing and the async setters at compile time, so there is no reflection and no runtime code generation.

Where a setter allocates:

- **No subscribers: 0 B.** The generated setter checks for subscribers before it creates the event args, so a change nobody listens to allocates nothing, for reference-type and value-type properties alike.
- **With subscribers: 0 B** for reference-type properties and for value types stored inline, when the handlers complete synchronously. The property event args are a `readonly struct`, and their old and new values are `PropertyValue`s, which store a reference-type value as the reference and a value type of up to 16 bytes without references inline: `int`, `double`, `decimal`, `Guid`, `DateTime`, `DateTimeOffset`, an enum, or a nullable of a type of at most 8 bytes such as `int?` or `DateTime?`. A larger struct, such as `Guid?` or `decimal?`, or a struct that contains references, is boxed once per value.
- **Handlers that do not complete synchronously** allocate what any awaited `ValueTask` does: the setter's async state machine, and whatever the handlers allocate.
- **Dispatch:** sequential and parallel dispatch add nothing when the handlers complete synchronously.

Before 2.0.0 the args were a `sealed class` with `object?` values: 40 B per raised event on 64-bit, plus a 24 B box for each `int` old and new value. [#237](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/issues/237) made them allocation-free.

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

Measured with version 1.3, before the args became a struct, on an otherwise idle machine, on .NET 10, out of process, in three launches pinned to two performance cores. Ratio is each row's mean time relative to the hand-written `INotifyPropertyChanged` in the same run, with the lowest and highest of the three launches in brackets. All four libraries have five handlers attached, to mirror a realistic MVVM scenario. CommunityToolkit.Mvvm 8.4.2, PropertyChanged.Fody 4.1.0.

| Library | Ratio, .NET 10 | Allocated | Async support |
|---|---:|---:|:---:|
| Manual `INotifyPropertyChanged` (baseline) | 1.00 | 24 B | ❌ |
| PropertyChanged.Fody | **0.93** (0.91–0.95) | **0 B** | ❌ |
| CommunityToolkit.Mvvm | 1.81 (1.74–1.91) | 0 B | ❌ |
| **ZeroAlloc.Notify** | **1.47** (1.42–1.54) | **0 B** | ✅ |

**Honest framing**: ZA.Notify is slower than a hand-written `INotifyPropertyChanged` and Fody. It is faster than CommunityToolkit.Mvvm here. The Notify row's ratio was measured with version 1.3, whose 40 B args object is gone in 2.0.0; its Allocated figure is the 2.0.0 one, which `AllocationTests` gates exactly. The view model also declares `PropertyChanging`, which has no subscribers here, so its args are never created. The other three are pure sync and cannot `await` propagation.

**ZA.Notify is the only library here that supports async handlers.** The trade-off is the cost of that capability. For pure-sync view models that never need to `await` a handler (the vast majority of XAML/Avalonia/WinUI scenarios), **Fody is the right choice**: it is as fast as hand-written code and allocates nothing. For scenarios where the setter needs to wait for async work to complete before returning (e.g., async validation before notifying observers; coordinated async state transitions), ZA.Notify is the only library that supports it.

The 2026-10-11 capture put ZA.Notify at 3.2 times the hand-written baseline. This run, three pinned launches on an idle machine, replaces it.

### Where the allocations come from

`NotifyAllocationBenchmarks`, same runs. The view model declares both `PropertyChanging` and `PropertyChanged`; "five subscribers" means five `PropertyChanged` handlers with the default parallel dispatch. Ratio is relative to the `string` setter with no subscribers, measured with version 1.3. The byte columns are the 2.0.0 figures.

| Setter | No subscribers | Five subscribers | Ratio, .NET 10 |
|---|---:|---:|---|
| `string` property | 0 B | 0 B | 1.00 → 2.06 (1.96–2.14) |
| `int` property | 0 B | 0 B | 0.47 (0.45–0.48) → 2.12 (1.92–2.45) |

Version 1.3 allocated 40 B for the `string` property with subscribers and 88 B for the `int` property: the args object plus a 24 B box each for the old and the new value.
<!-- BENCH:END -->

The test suite gates these numbers in `tests/ZeroAlloc.Notify.Tests/AllocationTests.cs` on .NET 10 and .NET 11, for sequential and parallel dispatch, so a regression fails the build.

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
        await __changed.InvokeAsync(AsyncPropertyChangedEventArgs.Create<int>(nameof(Count), __old, value), ct);
}
```

### 2. ValueTask

All async paths return `ValueTask`. When there are no subscribers, or every handler completes synchronously, the setter completes synchronously and its async state machine is never moved to the heap.

### 3. Struct Event Args with Unboxed Values

The property event args, `AsyncPropertyChangedEventArgs` and `AsyncPropertyChangingEventArgs`, are `readonly struct`s, so the args themselves never allocate. Their old and new values are `PropertyValue`s:

```csharp
public readonly struct AsyncPropertyChangedEventArgs
{
    public string PropertyName { get; }
    public PropertyValue OldValue { get; }
    public PropertyValue NewValue { get; }
}
```

A `PropertyValue` holds a reference-type value as the reference itself, and a value type of up to 16 bytes without references inline, tagged with its exact type. Read it with `As<T>()` or `TryGet<T>(out T)` using the property's type; neither boxes. `ToObject()` and reading as `object` or an interface box an inline value, on request only. The collection and error args are still classes.

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

- **Allocation**: INPC allocates a 24 B args object per notification; ZeroAlloc.Notify allocates nothing, with or without subscribers, under the conditions in the [overview](#overview)
- **Speed**: the async path took 1.47 times as long as the sync INPC baseline on .NET 10 in the 1.3 measurement; the cost buys full await semantics
- **Async support**: INPC has no built-in async support; ZeroAlloc.Notify has first-class `ValueTask` support
- **Full await**: INPC cannot await handlers; ZeroAlloc can

### vs Reactive Extensions (Rx)

- **Startup**: ZeroAlloc compiles all dispatch statically; Rx uses dynamic subscription
- **Allocation**: ZeroAlloc: 0 B per property change under the conditions in the [overview](#overview); Rx: Often 200+ bytes depending on operators
- **Use case**: ZeroAlloc is optimized for MVVM; Rx is general-purpose event streaming

### vs PropertyChanged.Fody

- **Dispatch**: Fody rewrites IL at build time; ZeroAlloc uses Roslyn source generation
- **Allocation**: both are 0 B per property change, ZeroAlloc under the conditions in the [overview](#overview); Fody is sync only
- **Async support**: Fody has no async handler dispatch; ZeroAlloc has first-class `ValueTask`
- **Full await**: Fody cannot await handlers; ZeroAlloc can

### vs MVVM Toolkit (CommunityToolkit.Mvvm)

- **Async support**: MVVM Toolkit has limited async support; ZeroAlloc has first-class async
- **Allocation pattern**: both allocate 0 B per property change, ZeroAlloc under the conditions in the [overview](#overview); ZeroAlloc's handlers are awaited
- **API surface**: MVVM Toolkit is broader (relaycommand, state management); ZeroAlloc is focused on notifications
- **Integration**: ZeroAlloc works standalone; MVVM Toolkit is part of a larger ecosystem

## Native AOT Compatibility

ZeroAlloc.Notify is fully compatible with Native AOT. Since all dispatch is resolved at compile time via the source generator, no reflection is needed at runtime. The package ships net10.0 and net11.0 builds marked `IsAotCompatible`, so the trim and AOT analyzers check the library itself:

```bash
dotnet publish -c Release -p PublishAot=true
```

## Memory Profiling

When using ZeroAlloc.Notify in your application, you'll notice:
- **Heap allocations**: none for property changes, with or without subscribers, as long as the handlers complete synchronously and the property type fits a `PropertyValue` inline or is a reference type
- **GC pressure**: from your handlers, and from the boxed fallback for large or reference-containing structs, not from the notifications themselves
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
