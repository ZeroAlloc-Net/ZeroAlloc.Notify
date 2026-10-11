using BenchmarkDotNet.Attributes;
using Notify.Benchmarks.Models;

namespace Notify.Benchmarks;

/// <summary>
/// Where a ZeroAlloc.Notify setter allocates. With no subscribers a change allocates nothing.
/// With subscribers each raised event allocates its args object, and a value-type property also
/// boxes the old and new value into it. The tests in AllocationTests gate these numbers.
/// </summary>
[MemoryDiagnoser]
public class NotifyAllocationBenchmarks
{
    private AsyncViewModel _unobserved = null!;
    private AsyncViewModel _observed = null!;
    private int _next;

    [GlobalSetup]
    public void Setup()
    {
        _unobserved = new AsyncViewModel();
        _observed = new AsyncViewModel();
        for (var i = 0; i < 5; i++)
            _observed.PropertyChangedAsync += (args, ct) => ValueTask.CompletedTask;
    }

    /// <summary>Reference-type property, nobody listening: expected 0 B.</summary>
    [Benchmark(Baseline = true)]
    public ValueTask SetName_NoSubscribers()
        => _unobserved.SetNameAsync(string.Equals(_unobserved.Name, "a", StringComparison.Ordinal) ? "b" : "a");

    /// <summary>Value-type property, nobody listening: expected 0 B, no boxing.</summary>
    [Benchmark]
    public ValueTask SetCount_NoSubscribers() => _unobserved.SetCountAsync(++_next);

    /// <summary>Reference-type property, five PropertyChanged handlers: the args object.</summary>
    [Benchmark]
    public ValueTask SetName_FiveSubscribers()
        => _observed.SetNameAsync(string.Equals(_observed.Name, "a", StringComparison.Ordinal) ? "b" : "a");

    /// <summary>Value-type property, five PropertyChanged handlers: the args object and two boxes.</summary>
    [Benchmark]
    public ValueTask SetCount_FiveSubscribers() => _observed.SetCountAsync(++_next);
}
