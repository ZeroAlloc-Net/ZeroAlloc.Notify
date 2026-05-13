# Performance

## Overview

ZeroAlloc.Notify is designed to minimize allocations on property and collection change notifications. By using Roslyn source generation and compile-time dispatch, all event invocations are inlined at the call site. Sync paths target zero allocation; the async dispatch path has modest overhead from the `ValueTask` state machine (48 B per notification in benchmarks).

## Benchmark Results

### Property Notifications — 4-way comparison

<!-- BENCH:START -->
_Last refreshed: 2026-05-13_

.NET 10.0.7, i9-12900HK, BenchmarkDotNet v0.15.8. All four libraries are configured with 5 attached handlers to mirror a realistic MVVM scenario.

| Library | Time | Allocated | Async support |
|---|---:|---:|:---:|
| Manual `INotifyPropertyChanged` (baseline) | 33.6 ns | 24 B | ❌ |
| PropertyChanged.Fody | **30.2 ns** | **0 B** | ❌ |
| CommunityToolkit.Mvvm | 55.2 ns | 0 B | ❌ |
| **ZeroAlloc.Notify** | **124.7 ns** | **80 B** | ✅ |

**Honest framing**: ZA.Notify is the slowest of the four and the only one that allocates. The 80 B is the `ValueTask` state machine for fan-out to async handlers. The other three are pure sync and can't `await` propagation.

**ZA.Notify is the only library here that supports async handlers.** The trade-off is the cost of that capability. For pure-sync view models that never need to `await` a handler (the vast majority of XAML/Avalonia/WinUI scenarios), **Fody is the right choice** — it's fastest and 0 B. For scenarios where the setter needs to wait for async work to complete before returning (e.g., async validation before notifying observers; coordinated async state transitions), ZA.Notify is the only library that supports it.

**Per-iteration scale**: even at 124.7 ns / 80 B per setter call, ZA.Notify can run **~8M property changes per second per thread** with ~640 MB/s of GC pressure. Most MVVM workloads have property change rates in the thousands per second, where the difference is invisible.
<!-- BENCH:END -->

> **Note:** Collection and validation benchmarks are not yet included in the benchmark suite. The property notification table above is the only measured result. See `benchmarks/Notify.Benchmarks/` to add more.

## Zero-Allocation Design

> The event args types (`AsyncPropertyChangedEventArgs`, etc.) are `sealed class` passed by reference. The async dispatch path itself has modest overhead from the `ValueTask` state machine.

### 1. Static Dispatch

All handler invocations are fully static — no virtual method calls, no dictionary lookups. The source generator creates concrete, inlined code:

```csharp
// Generated code for PropertyChangedAsync event
private async ValueTask OnPropertyChanged(string propertyName, object oldValue, object newValue)
{
    if (PropertyChangedAsync == null) return;
    
    var args = new AsyncPropertyChangedEventArgs(propertyName, oldValue, newValue);
    
    // Direct static invocation — no allocation
    await _handler1(args, default);
    await _handler2(args, default);
    // ... etc
}
```

### 2. ValueTask (No Boxing)

All async paths use `ValueTask<T>` to avoid boxing when handlers complete synchronously. The source generator never allocates a Task object for synchronous completion.

### 3. Sealed Class Event Args

Event argument types (`AsyncPropertyChangedEventArgs`, etc.) are `sealed class`, passed by reference across all handlers without copying:

```csharp
// AsyncPropertyChangedEventArgs — sealed class, passed by reference
public sealed class AsyncPropertyChangedEventArgs
{
    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
}
```

### 4. Compile-Time Code Generation

The Roslyn source generator analyzes your code at compile time and emits optimized dispatch code. No reflection happens at runtime.

## Benchmark Environment

- **Framework**: .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
- **Processor**: Intel Core i9-12900HK (cores: 16P+8E, base 3.3GHz)
- **OS**: Windows 11 Enterprise
- **BenchmarkDotNet Version**: 0.14.1
- **Measurement**: Mean of 10+ iterations

## Running Benchmarks

To run the benchmark suite yourself:

```bash
cd benchmarks/Notify.Benchmarks
dotnet run -c Release --framework net10.0
```

Results are output to `BenchmarkDotNet.Artifacts/results/`.

## Comparison with Alternatives

### vs INotifyPropertyChanged (Standard INPC)

- **Allocation**: ZeroAlloc async allocates 48 B; INPC allocates 24 B per notification (different operation — INPC is sync-only)
- **Speed**: Async path is 2.9x slower than sync INPC baseline — the cost buys full await semantics
- **Async support**: INPC has no built-in async support; ZeroAlloc.Notify has first-class `ValueTask` support
- **Full await**: INPC cannot await handlers; ZeroAlloc can

### vs Reactive Extensions (Rx)

- **Startup**: ZeroAlloc compiles all dispatch statically; Rx uses dynamic subscription
- **Allocation**: ZeroAlloc: 0B; Rx: Often 200+ bytes depending on operators
- **Use case**: ZeroAlloc is optimized for MVVM; Rx is general-purpose event streaming

### vs PropertyChanged.Fody

- **Dispatch**: Fody rewrites IL at build time; ZeroAlloc uses Roslyn source generation
- **Allocation**: Fody is 0 B (sync only); ZeroAlloc async is 48 B — the overhead of awaitable dispatch
- **Async support**: Fody has no async handler dispatch; ZeroAlloc has first-class `ValueTask`
- **Full await**: Fody cannot await handlers; ZeroAlloc can

### vs MVVM Toolkit (CommunityToolkit.Mvvm)

- **Async support**: MVVM Toolkit has limited async support; ZeroAlloc has first-class async
- **Allocation pattern**: MVVM Toolkit allocates 0 B for sync notifications; ZeroAlloc async allocates 48 B for awaitable dispatch
- **API surface**: MVVM Toolkit is broader (relaycommand, state management); ZeroAlloc is focused on notifications
- **Integration**: ZeroAlloc works standalone; MVVM Toolkit is part of a larger ecosystem

## Native AOT Compatibility

ZeroAlloc.Notify is fully compatible with Native AOT. Since all dispatch is resolved at compile time via the source generator, no reflection is needed at runtime:

```bash
dotnet publish -c Release -p PublishAot=true
```

## Memory Profiling

When using ZeroAlloc.Notify in your application, you'll notice:
- **Heap allocations**: Near-zero for property/collection notifications
- **GC pressure**: Minimal — less collections and shorter GC pauses
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
